using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class ForceDeletePostEndpoint
{
    public static RouteGroupBuilder MapForceDeletePost(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}/force", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            db.Posts.Remove(post);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("ForceDeletePost")
        .WithSummary("Permanently delete a post from database (Admin Only)");

        return group;
    }
}
