using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class UpdatePostStatusEndpoint
{
    public static RouteGroupBuilder MapUpdatePostStatus(this RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdatePostStatusRequest request,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var post = await db.Posts
                .Include(p => p.Author)
                .Include(p => p.Analytics)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            if (request.Status == PostStatus.Published && post.Status != PostStatus.Published && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.UtcNow;
            }

            post.Status = request.Status;
            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            return Results.Ok(PostHelpers.ToDetailDto(post, baseUrl));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdatePostStatus")
        .WithSummary("Transition post status between Draft, Published, Hidden, and Archived (Admin Only)");

        return group;
    }
}
