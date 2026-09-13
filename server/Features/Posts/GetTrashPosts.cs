using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class GetTrashPostsEndpoint
{
    public static RouteGroupBuilder MapGetTrashPosts(this RouteGroupBuilder group)
    {
        group.MapGet("/trash", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(10) <= 0 ? 10 : Math.Clamp(pageSize.GetValueOrDefault(10), 1, 50);

            var query = db.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Where(p => p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(term) ||
                                         (p.Summary != null && p.Summary.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var items = await query
                .OrderByDescending(p => p.DeletedAt ?? p.UpdatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(p => new TrashPostItemDto(
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.Summary,
                    p.Status,
                    p.DeletedAt,
                    p.CreatedAt,
                    new AuthorSummaryDto(p.Author.Id, p.Author.Username, p.Author.DisplayName, p.Author.AvatarUrl)
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<TrashPostItemDto>(items, currentPage, currentLimit, totalCount, totalPages));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetTrashPosts")
        .WithSummary("List all soft-deleted posts in the Trash bin (Admin Only)");

        return group;
    }
}
