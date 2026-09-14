using deblog.Server.Common.Entities;
using deblog.Server.Features.Analytics;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Media;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Common.Data;

public class AppDbContext : DbContext
{
    private readonly string _schema;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IConfiguration? configuration = null) : base(options)
    {
        _schema = configuration?["DB_SCHEMA"] ?? "deblog";
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserInformation> UserInformations => Set<UserInformation>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostAnalytics> PostAnalytics => Set<PostAnalytics>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<DailyTelemetry> DailyTelemetries => Set<DailyTelemetry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set custom schema (avoids polluting 'public' in shared Supabase instances)
        modelBuilder.HasDefaultSchema(_schema);

        // User configuration
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Username).IsUnique();
            builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
            builder.Property(u => u.Username).HasMaxLength(64).IsRequired();
            builder.Property(u => u.DisplayName).HasMaxLength(128);
            builder.Property(u => u.Bio).HasMaxLength(500);
            builder.Property(u => u.AvatarUrl).HasMaxLength(512);
            builder.Property(u => u.Role).HasMaxLength(32).HasDefaultValue(UserRoles.Guest);
            builder.Property(u => u.Status).HasConversion<int>().HasDefaultValue(UserStatus.Active);
            builder.Property(u => u.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(u => u.Status);
            builder.HasIndex(u => u.IsDeleted);
        });

        // Post configuration
        modelBuilder.Entity<Post>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => p.Slug).IsUnique();
            builder.Property(p => p.Title).HasMaxLength(256).IsRequired();
            builder.Property(p => p.Slug).HasMaxLength(300).IsRequired();
            builder.Property(p => p.Summary).HasMaxLength(500);
            builder.Property(p => p.Content).IsRequired();
            builder.Property(p => p.Status).HasConversion<int>().HasDefaultValue(PostStatus.Draft);
            builder.Property(p => p.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.IsDeleted);

            builder.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PostAnalytics configuration (1-to-1 with Post)
        modelBuilder.Entity<PostAnalytics>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.HasIndex(a => a.PostId).IsUnique();

            builder.HasOne(a => a.Post)
                .WithOne(p => p.Analytics)
                .HasForeignKey<PostAnalytics>(a => a.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Comment configuration
        modelBuilder.Entity<Comment>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Content).HasMaxLength(2000).IsRequired();
            builder.Property(c => c.Status).HasConversion<int>().HasDefaultValue(CommentStatus.Pending);
            builder.Property(c => c.ManagementToken).IsRequired();
            builder.Property(c => c.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(c => c.ManagementToken);
            builder.HasIndex(c => c.Status);
            builder.HasIndex(c => c.IsDeleted);

            builder.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserInformation configuration (1-to-1 with User)
        modelBuilder.Entity<UserInformation>(builder =>
        {
            builder.ToTable("user_information");
            builder.HasKey(ui => ui.Id);
            builder.HasIndex(ui => ui.UserId).IsUnique();
            builder.Property(ui => ui.JobTitle).HasMaxLength(128);
            builder.Property(ui => ui.Tagline).HasMaxLength(256);
            builder.Property(ui => ui.Location).HasMaxLength(128);
            builder.Property(ui => ui.BannerUrl).HasMaxLength(512);
            builder.Property(ui => ui.CopyrightYear).HasMaxLength(16);

            builder.HasOne(ui => ui.User)
                .WithOne(u => u.Information)
                .HasForeignKey<UserInformation>(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MediaItem configuration
        modelBuilder.Entity<MediaItem>(builder =>
        {
            builder.ToTable("media_items");
            builder.HasKey(m => m.Id);
            builder.HasIndex(m => m.PublicId).IsUnique();
            builder.Property(m => m.PublicId).HasMaxLength(256).IsRequired();
            builder.Property(m => m.SecureUrl).HasMaxLength(1024).IsRequired();
            builder.Property(m => m.Filename).HasMaxLength(256).IsRequired();
            builder.Property(m => m.MimeType).HasMaxLength(64).IsRequired();
            builder.Property(m => m.AltText).HasMaxLength(256);

            builder.HasOne(m => m.UploadedBy)
                .WithMany()
                .HasForeignKey(m => m.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DailyTelemetry configuration
        modelBuilder.Entity<DailyTelemetry>(builder =>
        {
            builder.ToTable("daily_telemetry");
            builder.HasKey(dt => dt.Id);
            builder.HasIndex(dt => dt.Date).IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
