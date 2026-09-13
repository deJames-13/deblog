using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class CreatePostEndpoint
{
    public static RouteGroupBuilder MapCreatePost(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            ClaimsPrincipal user,
            [FromBody] CreatePostRequest request,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var userEmail = user.GetUserEmail();

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { message = "Title is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { message = "Content is required" });
            }

            // Find or associate the Author in a single efficient query
            var normalizedEmail = userEmail?.Trim().ToLowerInvariant();
            var author = await db.Users
                .Where(u => (userId.HasValue && u.Id == userId.Value) ||
                            (normalizedEmail != null && u.Email.ToLower() == normalizedEmail) ||
                            u.Role == UserRoles.Admin)
                .OrderByDescending(u => userId.HasValue && u.Id == userId.Value)
                .ThenByDescending(u => normalizedEmail != null && u.Email.ToLower() == normalizedEmail)
                .FirstOrDefaultAsync(ct);

            if (author == null)
            {
                return Results.BadRequest(new { message = "Author profile not found. Ensure main author is seeded." });
            }

            var baseSlug = !string.IsNullOrWhiteSpace(request.Slug)
                ? PostHelpers.GenerateSlug(request.Slug)
                : PostHelpers.GenerateSlug(request.Title);

            var existingSlugs = await db.Posts
                .Where(p => p.Slug == baseSlug || p.Slug.StartsWith(baseSlug + "-"))
                .Select(p => p.Slug)
                .ToListAsync(ct);

            var slug = baseSlug;
            if (existingSlugs.Contains(baseSlug))
            {
                var suffix = 1;
                while (existingSlugs.Contains($"{baseSlug}-{suffix}"))
                {
                    suffix++;
                }
                slug = $"{baseSlug}-{suffix}";
            }

            // Determine status (support both Status and legacy IsPublished flag)
            var status = request.Status;
            if (request.IsPublished.HasValue)
            {
                status = request.IsPublished.Value ? PostStatus.Published : PostStatus.Draft;
            }

            var now = DateTime.UtcNow;
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Slug = slug,
                Summary = request.Summary?.Trim(),
                Content = request.Content,
                Status = status,
                PublishedAt = status == PostStatus.Published ? now : null,
                CoverImageUrl = request.CoverImageUrl?.Trim(),
                Category = request.Category?.Trim(),
                Tags = request.Tags ?? [],
                IsFeatured = request.IsFeatured,
                AuthorId = author.Id,
                Author = author,
                IsDeleted = false,
                Analytics = new PostAnalytics
                {
                    Views = 0,
                    Likes = 0,
                    Shares = 0
                }
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            return Results.Created($"/api/posts/{post.Slug}", PostHelpers.ToDetailDto(post, baseUrl));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreatePost")
        .WithSummary("Create a new blog post as Draft or Published (Admin Only)");

        return group;
    }
}
