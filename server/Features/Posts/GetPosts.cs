using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class GetPostsEndpoint
{
    public static RouteGroupBuilder MapGetPosts(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            ClaimsPrincipal user,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? tag,
            [FromQuery] Guid? authorId,
            [FromQuery] PostStatus? status,
            [FromQuery] bool? publishedOnly,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(10) <= 0 ? 10 : Math.Clamp(pageSize.GetValueOrDefault(10), 1, 50);
            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            var isAdmin = user.IsAdmin(config);

            var baseQuery = db.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            if (!isAdmin)
            {
                // Public visitors only see published posts
                baseQuery = baseQuery.Where(p => p.Status == PostStatus.Published);
            }
            else
            {
                // Admin can filter by specific status
                if (status.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.Status == status.Value);
                }
                else if (publishedOnly == true)
                {
                    baseQuery = baseQuery.Where(p => p.Status == PostStatus.Published);
                }
            }

            if (authorId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.AuthorId == authorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catTerm = category.Trim().ToLower();
                baseQuery = baseQuery.Where(p => p.Category != null && p.Category.ToLower() == catTerm);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var tagTerm = tag.Trim().ToLower();
                baseQuery = baseQuery.Where(p => p.Tags.Any(t => t.ToLower() == tagTerm));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                baseQuery = baseQuery.Where(p => p.Title.ToLower().Contains(term) ||
                                         (p.Summary != null && p.Summary.ToLower().Contains(term)));
            }

            var totalCount = await baseQuery.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var posts = await baseQuery
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Include(p => p.Author)
                .Include(p => p.Analytics)
                .Select(p => PostHelpers.ToListItemDto(p, baseUrl))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<PostListItemDto>(posts, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("GetPosts")
        .WithSummary("Get paginated list of posts with status filtering (excludes soft-deleted)");

        return group;
    }
}
