using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class GetApprovedPostCommentsEndpoint
{
    public static RouteGroupBuilder MapGetApprovedPostComments(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            Guid postId,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var postExists = await db.Posts.AnyAsync(p => p.Id == postId && !p.IsDeleted, ct);
            if (!postExists)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            var comments = await db.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.Status == CommentStatus.Approved && !c.IsDeleted)
                .Include(c => c.Author)
                .OrderBy(c => c.CreatedAt)
                .Select(c => CommentHelpers.ToResponseDto(c))
                .ToListAsync(ct);

            return Results.Ok(comments);
        })
        .WithName("GetApprovedPostComments")
        .WithSummary("Get all approved, non-deleted comments for a specific post");

        return group;
    }
}
