using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class UpdatePostEndpoint
{
    public static RouteGroupBuilder MapUpdatePost(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdatePostRequest request,
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

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                post.Title = request.Title.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                var newSlug = PostHelpers.GenerateSlug(request.Slug);
                if (newSlug != post.Slug && await db.Posts.AnyAsync(p => p.Slug == newSlug && p.Id != id, ct))
                {
                    return Results.BadRequest(new { message = "Slug is already in use" });
                }
                post.Slug = newSlug;
            }

            if (request.Summary != null)
            {
                post.Summary = request.Summary.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                post.Content = request.Content;
            }

            if (request.Status.HasValue)
            {
                if (request.Status.Value == PostStatus.Published && post.Status != PostStatus.Published && post.PublishedAt == null)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }
                post.Status = request.Status.Value;
            }
            else if (request.IsPublished.HasValue)
            {
                var targetStatus = request.IsPublished.Value ? PostStatus.Published : PostStatus.Draft;
                if (targetStatus == PostStatus.Published && post.Status != PostStatus.Published && post.PublishedAt == null)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }
                post.Status = targetStatus;
            }

            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            return Results.Ok(PostHelpers.ToDetailDto(post, baseUrl));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdatePost")
        .WithSummary("Update post details and status (Admin Only)");

        return group;
    }
}
