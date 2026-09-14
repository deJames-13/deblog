using deblog.Server.Common.Data;
using deblog.Server.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Media;

public static class DeleteMediaEndpoint
{
    public static RouteGroupBuilder MapDeleteMedia(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICloudinaryService cloudinary,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var item = await db.MediaItems.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (item == null)
            {
                return Results.NotFound(new { message = "Media asset not found." });
            }

            // Remove from Cloudinary CDN
            if (!string.IsNullOrWhiteSpace(item.PublicId))
            {
                await cloudinary.DeleteImageAsync(item.PublicId, ct);
            }

            db.MediaItems.Remove(item);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Media asset deleted successfully." });
        })
        .RequireAuthorization("AdminOnly")
        .WithName("DeleteMedia")
        .WithSummary("Delete media asset from Cloudinary and database (Admin Only)");

        return group;
    }
}
