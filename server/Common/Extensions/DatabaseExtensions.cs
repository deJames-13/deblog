using deblog.Server.Common.Data;
using deblog.Server.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Common.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DB_CONNECTION"]
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Database=deblog;Username=postgres;Password=postgres";
        }

        if (configuration.GetValue<bool>("USE_IN_MEMORY_DB", false))
        {
            var inMemoryDbName = configuration["IN_MEMORY_DB_NAME"] ?? "DeblogInMemoryDb";
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(inMemoryDbName);
            });
            return services;
        }

        var schema = configuration["DB_SCHEMA"] ?? "deblog";

        if (!connectionString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = $"{connectionString.TrimEnd(';')};Pooling=true;Minimum Pool Size=1;Maximum Pool Size=20;Connection Idle Lifetime=300;Keepalive=30;";
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            });
        });

        return services;
    }

    public static async Task SeedMainAuthorAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        var adminEmail = config["AUTHOR_EMAIL"] ?? config["ADMIN_EMAIL"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogInformation("No AUTHOR_EMAIL or ADMIN_EMAIL provided in environment; skipping main author seeding.");
            return;
        }

        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        var existingUser = await db.Users
            .Include(u => u.Information)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (existingUser == null)
        {
            var username = config["AUTHOR_USERNAME"] ?? config["ADMIN_USERNAME"] ?? normalizedEmail.Split('@')[0];
            var displayName = config["AUTHOR_DISPLAYNAME"] ?? config["ADMIN_DISPLAY_NAME"] ?? username;
            var bio = config["AUTHOR_BIO"] ?? config["ADMIN_BIO"] ?? "Main Author & Creator of deblog";
            var avatarUrl = config["AUTHOR_AVATARURL"] ?? config["ADMIN_AVATAR_URL"];

            var adminId = Guid.TryParse(config["AUTHOR_USER_ID"] ?? config["ADMIN_USER_ID"], out var parsedId)
                ? parsedId
                : Guid.NewGuid();

            var adminUser = new User
            {
                Id = adminId,
                Email = normalizedEmail,
                Username = username,
                DisplayName = displayName,
                Bio = bio,
                AvatarUrl = avatarUrl,
                Role = UserRoles.Admin,
                Information = new UserInformation
                {
                    Id = Guid.NewGuid(),
                    UserId = adminId,
                    JobTitle = "Power Platform Developer | Full Stack Developer",
                    Tagline = "Engineering enterprise automations, clean full-stack backends, and low-latency minimalist interfaces.",
                    Location = "Manila, Philippines / Remote",
                    BannerUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=1200&q=80",
                    CopyrightYear = "2026",
                    SocialLinksJson = "{\"github\":\"https://github.com/deJames-13\",\"facebook\":\"https://facebook.com/the2ndpercyfied\",\"linkedin\":\"https://linkedin.com/in/derickjamesespinosa\",\"instagram\":\"https://instagram.com/_doftenfools\"}"
                }
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
            logger.LogInformation("Successfully seeded main author profile: {Email} ({Username}) with Role: Admin and UserInformation", normalizedEmail, username);
        }
        else
        {
            var modified = false;
            if (existingUser.Role != UserRoles.Admin)
            {
                existingUser.Role = UserRoles.Admin;
                modified = true;
                logger.LogInformation("Updated existing user {Email} role to Admin.", normalizedEmail);
            }

            if (existingUser.Information == null)
            {
                var newInfo = new UserInformation
                {
                    Id = Guid.NewGuid(),
                    UserId = existingUser.Id,
                    JobTitle = "Power Platform Developer | Full Stack Developer",
                    Tagline = "Engineering enterprise automations, clean full-stack backends, and low-latency minimalist interfaces.",
                    Location = "Manila, Philippines / Remote",
                    BannerUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=1200&q=80",
                    CopyrightYear = "2026",
                    SocialLinksJson = "{\"github\":\"https://github.com/deJames-13\",\"facebook\":\"https://facebook.com/the2ndpercyfied\",\"linkedin\":\"https://linkedin.com/in/derickjamesespinosa\",\"instagram\":\"https://instagram.com/_doftenfools\"}"
                };
                db.UserInformations.Add(newInfo);
                existingUser.Information = newInfo;
                modified = true;
                logger.LogInformation("Seeded default UserInformation for existing author: {Email}.", normalizedEmail);
            }

            if (modified)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
