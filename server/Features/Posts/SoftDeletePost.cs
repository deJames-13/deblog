using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class SoftDeletePostEndpoint
{
    public static RouteGroupBuilder MapSoftDeletePost(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            post.IsDeleted = true;
            post.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SoftDeletePost")
        .WithSummary("Move a post to trash / soft-delete (Admin Only)");

        return group;
    }
}
