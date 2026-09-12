using System.Security.Claims;
using System.Text.RegularExpressions;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Common.Services;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts")
            .WithTags("Posts");

        // GET /api/posts - List posts with pagination, filtering, and analytics
        group.MapGet("/", async (
            ClaimsPrincipal user,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] Guid? authorId,
            [FromQuery] bool? publishedOnly,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(10) <= 0 ? 10 : Math.Clamp(pageSize.GetValueOrDefault(10), 1, 50);
            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            var isAdmin = user.IsAdmin(config);

            var query = db.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Analytics)
                .Include(p => p.Comments)
                .AsQueryable();

            // Only Admin can see unpublished posts
            if (!isAdmin || publishedOnly != false)
            {
                query = query.Where(p => p.IsPublished);
            }

            if (authorId.HasValue)
            {
                query = query.Where(p => p.AuthorId == authorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(term) ||
                                         (p.Summary != null && p.Summary.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var posts = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(p => new PostListItemDto(
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Summary,
                    $"{baseUrl}/posts/{p.Slug}",
                    p.IsPublished,
                    p.PublishedAt,
                    p.CreatedAt,
                    new AuthorSummaryDto(p.Author.Id, p.Author.Username, p.Author.DisplayName, p.Author.AvatarUrl),
                    new PostAnalyticsDto(
                        p.Analytics != null ? p.Analytics.Views : 0,
                        p.Analytics != null ? p.Analytics.Likes : 0,
                        p.Analytics != null ? p.Analytics.Shares : 0,
                        p.Comments.Count(c => c.Status == CommentStatus.Approved)
                    )
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<PostListItemDto>(posts, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("GetPosts")
        .WithSummary("Get paginated list of posts with analytics");

        // GET /api/posts/{idOrSlug} - Get single post with analytics
        group.MapGet("/{idOrSlug}", async (
            string idOrSlug,
            ClaimsPrincipal user,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            var isAdmin = user.IsAdmin(config);

            var query = db.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Analytics)
                .Include(p => p.Comments)
                .AsQueryable();

            Post? post;
            if (Guid.TryParse(idOrSlug, out var id))
            {
                post = await query.FirstOrDefaultAsync(p => p.Id == id, ct);
            }
            else
            {
                post = await query.FirstOrDefaultAsync(p => p.Slug == idOrSlug.ToLower(), ct);
            }

            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            if (!post.IsPublished && !isAdmin)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            var dto = new PostDetailDto(
                post.Id,
                post.Title,
                post.Slug,
                post.Summary,
                post.Content,
                $"{baseUrl}/posts/{post.Slug}",
                post.IsPublished,
                post.PublishedAt,
                post.CreatedAt,
                post.UpdatedAt,
                new AuthorSummaryDto(post.Author.Id, post.Author.Username, post.Author.DisplayName, post.Author.AvatarUrl),
                new PostAnalyticsDto(
                    post.Analytics?.Views ?? 0,
                    post.Analytics?.Likes ?? 0,
                    post.Analytics?.Shares ?? 0,
                    post.Comments.Count(c => c.Status == CommentStatus.Approved)
                )
            );

            return Results.Ok(dto);
        })
        .WithName("GetPostByIdOrSlug")
        .WithSummary("Get post details by ID or Slug with canonical URL and analytics");

        // POST /api/posts - Create post (Admin Only)
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

            // Find or associate the Author
            User? author = null;
            if (userId.HasValue)
            {
                author = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
            }
            if (author == null && !string.IsNullOrWhiteSpace(userEmail))
            {
                author = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), ct);
            }
            if (author == null)
            {
                // Fallback to seeded main author
                author = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRoles.Admin, ct);
            }

            if (author == null)
            {
                return Results.BadRequest(new { message = "Author profile not found. Ensure main author is seeded." });
            }

            var baseSlug = !string.IsNullOrWhiteSpace(request.Slug)
                ? GenerateSlug(request.Slug)
                : GenerateSlug(request.Title);

            var slug = baseSlug;
            var slugSuffix = 1;
            while (await db.Posts.AnyAsync(p => p.Slug == slug, ct))
            {
                slug = $"{baseSlug}-{slugSuffix++}";
            }

            var now = DateTime.UtcNow;
            var post = new Post
            {
                Title = request.Title.Trim(),
                Slug = slug,
                Summary = request.Summary?.Trim(),
                Content = request.Content,
                IsPublished = request.IsPublished,
                PublishedAt = request.IsPublished ? now : null,
                AuthorId = author.Id,
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
            var responseDto = new PostDetailDto(
                post.Id,
                post.Title,
                post.Slug,
                post.Summary,
                post.Content,
                $"{baseUrl}/posts/{post.Slug}",
                post.IsPublished,
                post.PublishedAt,
                post.CreatedAt,
                post.UpdatedAt,
                new AuthorSummaryDto(author.Id, author.Username, author.DisplayName, author.AvatarUrl),
                new PostAnalyticsDto(0, 0, 0, 0)
            );

            return Results.Created($"/api/posts/{post.Slug}", responseDto);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreatePost")
        .WithSummary("Create a new blog post (Admin Only)");

        // PUT /api/posts/{id:guid} - Update post (Admin Only)
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
                .FirstOrDefaultAsync(p => p.Id == id, ct);

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
                var newSlug = GenerateSlug(request.Slug);
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

            if (request.IsPublished.HasValue)
            {
                if (request.IsPublished.Value && !post.IsPublished)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }
                post.IsPublished = request.IsPublished.Value;
            }

            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            var responseDto = new PostDetailDto(
                post.Id,
                post.Title,
                post.Slug,
                post.Summary,
                post.Content,
                $"{baseUrl}/posts/{post.Slug}",
                post.IsPublished,
                post.PublishedAt,
                post.CreatedAt,
                post.UpdatedAt,
                new AuthorSummaryDto(post.Author.Id, post.Author.Username, post.Author.DisplayName, post.Author.AvatarUrl),
                new PostAnalyticsDto(
                    post.Analytics?.Views ?? 0,
                    post.Analytics?.Likes ?? 0,
                    post.Analytics?.Shares ?? 0,
                    post.Comments.Count(c => c.Status == CommentStatus.Approved)
                )
            );

            return Results.Ok(responseDto);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdatePost")
        .WithSummary("Update a post (Admin Only)");

        // DELETE /api/posts/{id:guid} - Delete post (Admin Only)
        group.MapDelete("/{id:guid}", async (
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
        .WithName("DeletePost")
        .WithSummary("Delete a post (Admin Only)");

        // ==================== Analytics Endpoints ====================

        // POST /api/posts/{idOrSlug}/analytics/view
        group.MapPost("/{idOrSlug}/analytics/view", async (
            string idOrSlug,
            HttpContext httpContext,
            IAnalyticsTracker tracker,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await ResolvePostWithAnalyticsAsync(idOrSlug, db, ct);
            if (post == null) return Results.NotFound(new { message = "Post not found" });

            var clientId = tracker.GetClientIdentifier(httpContext);
            if (tracker.CanTrackView(clientId, post.Id))
            {
                post.Analytics ??= new PostAnalytics { PostId = post.Id };
                post.Analytics.Views++;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { views = post.Analytics?.Views ?? 0 });
        })
        .WithName("TrackPostView")
        .WithSummary("Increment post view counter with visitor cooldown deduplication");

        // POST /api/posts/{idOrSlug}/analytics/like
        group.MapPost("/{idOrSlug}/analytics/like", async (
            string idOrSlug,
            HttpContext httpContext,
            IAnalyticsTracker tracker,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await ResolvePostWithAnalyticsAsync(idOrSlug, db, ct);
            if (post == null) return Results.NotFound(new { message = "Post not found" });

            var clientId = tracker.GetClientIdentifier(httpContext);
            if (tracker.CanTrackLike(clientId, post.Id))
            {
                post.Analytics ??= new PostAnalytics { PostId = post.Id };
                post.Analytics.Likes++;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { likes = post.Analytics?.Likes ?? 0 });
        })
        .WithName("TrackPostLike")
        .WithSummary("Increment post like counter with visitor cooldown deduplication");

        // POST /api/posts/{idOrSlug}/analytics/share
        group.MapPost("/{idOrSlug}/analytics/share", async (
            string idOrSlug,
            HttpContext httpContext,
            IAnalyticsTracker tracker,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await ResolvePostWithAnalyticsAsync(idOrSlug, db, ct);
            if (post == null) return Results.NotFound(new { message = "Post not found" });

            var clientId = tracker.GetClientIdentifier(httpContext);
            if (tracker.CanTrackShare(clientId, post.Id))
            {
                post.Analytics ??= new PostAnalytics { PostId = post.Id };
                post.Analytics.Shares++;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { shares = post.Analytics?.Shares ?? 0 });
        })
        .WithName("TrackPostShare")
        .WithSummary("Increment post share counter with visitor cooldown deduplication");

        return app;
    }

    private static async Task<Post?> ResolvePostWithAnalyticsAsync(string idOrSlug, AppDbContext db, CancellationToken ct)
    {
        if (Guid.TryParse(idOrSlug, out var id))
        {
            return await db.Posts
                .Include(p => p.Analytics)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        return await db.Posts
            .Include(p => p.Analytics)
            .FirstOrDefaultAsync(p => p.Slug == idOrSlug.ToLower(), ct);
    }

    private static string GenerateSlug(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString()[..8] : slug;
    }
}
