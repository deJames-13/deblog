using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Common.Services;
using deblog.Server.Features.Media;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace deblog.Server.Features.Settings;

public static class UploadAvatarEndpoint
{
    public static RouteGroupBuilder MapUploadAvatar(this RouteGroupBuilder group)
    {
        group.MapPost("/avatar", async (
            HttpRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            IConfiguration config,
            ICloudinaryService cloudinaryService,
            CancellationToken ct) =>
        {
            if (!cloudinaryService.IsConfigured)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Cloudinary Not Configured",
                    detail: "Cloudinary credentials are not configured on the server. Image uploads are unavailable."
                );
            }

            if (!request.HasFormContentType)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status415UnsupportedMediaType,
                    title: "Unsupported Media Type",
                    detail: "Request must be multipart/form-data."
                );
            }

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");

            if (file == null || file.Length == 0)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Missing File",
                    detail: "No image file was provided in the multipart form under the 'file' key."
                );
            }

            if (file.Length > CloudinaryService.MaxFileSizeBytes)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: "File Too Large",
                    detail: $"The uploaded avatar ({file.Length / 1024} KB) exceeds the 1MB limit."
                );
            }

            // Upload to Cloudinary CDN
            CloudinaryUploadResult uploadResult;
            try
            {
                await using var stream = file.OpenReadStream();
                uploadResult = await cloudinaryService.UploadImageAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    folder: "deblog/avatars",
                    ct: ct
                );
            }
            catch (BadHttpRequestException ex)
            {
                return Results.Problem(statusCode: ex.StatusCode, title: "Image Validation Failed", detail: ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "Upload Failed", detail: ex.Message);
            }

            // Find Admin author
            var userId = principal.GetUserId();
            var adminEmail = config["AUTHOR_EMAIL"]?.Trim().ToLowerInvariant();

            var user = await db.Users
                .FirstOrDefaultAsync(u =>
                    (userId.HasValue && u.Id == userId.Value) ||
                    (!string.IsNullOrWhiteSpace(adminEmail) && u.Email.ToLower() == adminEmail) ||
                    u.Role == UserRoles.Admin, ct);

            if (user != null)
            {
                user.AvatarUrl = uploadResult.SecureUrl;

                // Also record into media library
                var mediaItem = new MediaItem
                {
                    Id = Guid.NewGuid(),
                    PublicId = uploadResult.PublicId,
                    SecureUrl = uploadResult.SecureUrl,
                    Filename = file.FileName,
                    MimeType = file.ContentType,
                    FileSizeBytes = uploadResult.Bytes,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    AltText = "Profile Avatar",
                    UploadedById = user.Id
                };
                db.MediaItems.Add(mediaItem);

                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new UploadSettingAssetResponse(
                Url: uploadResult.SecureUrl,
                PublicId: uploadResult.PublicId,
                Message: "Avatar image uploaded to Cloudinary CDN and linked successfully."
            ));
        })
        .RequireAuthorization("AdminOnly")
        .DisableAntiforgery()
        .WithName("UploadSettingsAvatar")
        .WithSummary("Upload and update author avatar to Cloudinary CDN");

        return group;
    }
}
