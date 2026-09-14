using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Common.Services;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace deblog.Server.Features.Settings;

public static class UpdateSettingsEndpoint
{
    public static RouteGroupBuilder MapUpdateSettings(this RouteGroupBuilder group)
    {
        group.MapPut("/", async (
            ClaimsPrincipal principal,
            [FromBody] UpdateSiteSettingsRequest request,
            AppDbContext db,
            IConfiguration config,
            ICloudinaryService cloudinaryService,
            CancellationToken ct) =>
        {
            // Strictly guard against storing base64 image payloads in the database
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
                request.AvatarUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Image URL",
                    detail: "Base64 image data is not permitted in the database. Only Cloudinary CDN URLs are allowed."
                );
            }

            if (!string.IsNullOrWhiteSpace(request.BannerUrl) &&
                request.BannerUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Image URL",
                    detail: "Base64 image data is not permitted in the database. Only Cloudinary CDN URLs are allowed."
                );
            }

            var userId = principal.GetUserId();
            var adminEmail = config["AUTHOR_EMAIL"]?.Trim().ToLowerInvariant();

            var user = await db.Users
                .Include(u => u.Information)
                .FirstOrDefaultAsync(u =>
                    (userId.HasValue && u.Id == userId.Value) ||
                    (!string.IsNullOrWhiteSpace(adminEmail) && u.Email.ToLower() == adminEmail) ||
                    u.Role == UserRoles.Admin, ct);

            if (user == null)
            {
                return Results.NotFound(new { message = "Site author settings not found." });
            }

            if (request.DisplayName != null) user.DisplayName = request.DisplayName.Trim();
            if (request.Bio != null) user.Bio = request.Bio.Trim();
            if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl.Trim();

            if (user.Information == null)
            {
                var newInfo = new UserInformation
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id
                };
                db.UserInformations.Add(newInfo);
                user.Information = newInfo;
            }

            if (request.Role != null) user.Information.JobTitle = request.Role.Trim();
            if (request.Tagline != null) user.Information.Tagline = request.Tagline.Trim();
            if (request.Location != null) user.Information.Location = request.Location.Trim();
            if (request.BannerUrl != null) user.Information.BannerUrl = request.BannerUrl.Trim();
            if (request.CopyrightYear != null) user.Information.CopyrightYear = request.CopyrightYear.Trim();
            if (request.SocialLinksJson != null) user.Information.SocialLinksJson = request.SocialLinksJson.Trim();

            await db.SaveChangesAsync(ct);

            var response = new SiteSettingsResponse(
                UserId: user.Id,
                Email: user.Email,
                Username: user.Username,
                DisplayName: user.DisplayName ?? user.Username,
                Role: user.Information.JobTitle,
                Tagline: user.Information.Tagline,
                Bio: user.Bio,
                Location: user.Information.Location,
                AvatarUrl: user.AvatarUrl,
                BannerUrl: user.Information.BannerUrl,
                CopyrightYear: user.Information.CopyrightYear ?? DateTime.UtcNow.Year.ToString(),
                SocialLinksJson: user.Information.SocialLinksJson,
                CloudinaryConfigured: cloudinaryService.IsConfigured
            );

            return Results.Ok(response);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateSiteSettings")
        .WithSummary("Update site author profile, visual branding, and coordinates");

        return group;
    }
}
