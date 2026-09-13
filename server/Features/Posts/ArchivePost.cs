using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class ArchivePostEndpoint
{
    public static RouteGroupBuilder MapArchivePost(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
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

            post.Status = PostStatus.Archived;
            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            return Results.Ok(PostHelpers.ToDetailDto(post, baseUrl));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("ArchivePost")
        .WithSummary("Archive a post for historical preservation (Admin Only)");

        return group;
    }
}
