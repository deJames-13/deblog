using deblog.Server.Common.Data;
using deblog.Server.Common.Services;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace deblog.Server.Features.Settings;

public static class GetSettingsEndpoint
{
    public static RouteGroupBuilder MapGetSettings(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            AppDbContext db,
            IConfiguration config,
            ICloudinaryService cloudinaryService,
            CancellationToken ct) =>
        {
            var adminEmail = config["AUTHOR_EMAIL"]?.Trim().ToLowerInvariant();
            var user = await db.Users
                .Include(u => u.Information)
                .FirstOrDefaultAsync(u =>
                    (!string.IsNullOrWhiteSpace(adminEmail) && u.Email.ToLower() == adminEmail) ||
                    u.Role == UserRoles.Admin, ct);

            if (user == null)
            {
                return Results.NotFound(new { message = "Site author settings not found." });
            }

            var response = new SiteSettingsResponse(
                UserId: user.Id,
                Email: user.Email,
                Username: user.Username,
                DisplayName: user.DisplayName ?? user.Username,
                Role: user.Information?.JobTitle,
                Tagline: user.Information?.Tagline,
                Bio: user.Bio,
                Location: user.Information?.Location,
                AvatarUrl: user.AvatarUrl,
                BannerUrl: user.Information?.BannerUrl,
                CopyrightYear: user.Information?.CopyrightYear ?? DateTime.UtcNow.Year.ToString(),
                SocialLinksJson: user.Information?.SocialLinksJson,
                CloudinaryConfigured: cloudinaryService.IsConfigured
            );

            return Results.Ok(response);
        })
        .WithName("GetSiteSettings")
        .WithSummary("Get site profile and branding settings");

        return group;
    }
}
