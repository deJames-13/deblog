using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Media;

public static class MediaEndpoints
{
    public static RouteGroupBuilder MapMediaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/media")
            .WithTags("Media");

        group.MapGet("/status", (ICloudinaryService cloudinary) => Results.Ok(new
        {
            configured = cloudinary.IsConfigured,
            status = cloudinary.IsConfigured ? "online" : "offline",
            maxFileSizeKb = 1024,
            allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/avif" }
        }))
        .WithName("GetMediaStatus")
        .WithSummary("Check media storage connectivity and configuration status");

        group.MapUploadMedia();
        group.MapListMedia();
        group.MapDeleteMedia();

        return group;
    }
}
