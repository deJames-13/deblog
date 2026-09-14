using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace deblog.Server.Features.Media;

public static class UploadMediaEndpoint
{
    public static RouteGroupBuilder MapUploadMedia(this RouteGroupBuilder group)
    {
        group.MapPost("/upload", async (
            HttpRequest request,
            ClaimsPrincipal user,
            ICloudinaryService cloudinary,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!cloudinary.IsConfigured)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Media Storage Offline",
                    detail: "Cloudinary is not configured. Media uploads are currently unavailable."
                );
            }

            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest(new { message = "Multipart form data with an image file is required." });
            }

            var file = request.Form.Files["file"] ?? request.Form.Files[0];
            if (file.Length == 0)
            {
                return Results.BadRequest(new { message = "The uploaded file is empty." });
            }

            if (file.Length > CloudinaryService.MaxFileSizeBytes)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    title: "Payload Too Large",
                    detail: $"Image file size ({file.Length / 1024} KB) exceeds the maximum allowed limit of 1MB."
                );
            }

            var altText = request.Form["altText"].ToString();
            var userId = user.GetUserId();

            try
            {
                await using var stream = file.OpenReadStream();
                var uploadResult = await cloudinary.UploadImageAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    "deblog/assets",
                    ct
                );

                var mediaItem = new MediaItem
                {
                    Id = Guid.NewGuid(),
                    PublicId = uploadResult.PublicId,
                    SecureUrl = uploadResult.SecureUrl,
                    Filename = file.FileName,
                    MimeType = file.ContentType,
                    FileSizeBytes = uploadResult.Bytes > 0 ? uploadResult.Bytes : file.Length,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    AltText = !string.IsNullOrWhiteSpace(altText)
                        ? altText.Trim()
                        : Path.GetFileNameWithoutExtension(file.FileName),
                    UploadedById = userId
                };

                db.MediaItems.Add(mediaItem);
                await db.SaveChangesAsync(ct);

                var dto = new MediaItemDto(
                    mediaItem.Id,
                    mediaItem.PublicId,
                    mediaItem.SecureUrl,
                    mediaItem.Filename,
                    mediaItem.MimeType,
                    mediaItem.FileSizeBytes,
                    Math.Max(1, mediaItem.FileSizeBytes / 1024),
                    mediaItem.Width,
                    mediaItem.Height,
                    $"{mediaItem.Width}x{mediaItem.Height}",
                    mediaItem.AltText,
                    mediaItem.CreatedAt
                );

                return Results.Created($"/api/media/{mediaItem.Id}", new UploadMediaResponse(dto, "Asset uploaded successfully to Cloudinary."));
            }
            catch (BadHttpRequestException ex)
            {
                return Results.Problem(
                    statusCode: ex.StatusCode,
                    title: "Invalid Image",
                    detail: ex.Message
                );
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Upload Failed",
                    detail: ex.Message
                );
            }
        })
        .DisableAntiforgery()
        .RequireAuthorization("AdminOnly")
        .WithName("UploadMedia")
        .WithSummary("Upload an image asset to Cloudinary (max 1MB, multipart/form-data)");

        return group;
    }
}
