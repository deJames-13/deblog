using deblog.Server.Common.Entities;
using deblog.Server.Features.Comments;
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
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostAnalytics> PostAnalytics => Set<PostAnalytics>();
    public DbSet<Comment> Comments => Set<Comment>();

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
            builder.HasIndex(c => c.ManagementToken);
            builder.HasIndex(c => c.Status);

            builder.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
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
