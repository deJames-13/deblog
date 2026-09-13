using deblog.Server.Common.Data;
using deblog.Server.Features.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class AdminGetTrashCommentsEndpoint
{
    public static RouteGroupBuilder MapAdminGetTrashComments(this RouteGroupBuilder adminCommentsGroup)
    {
        adminCommentsGroup.MapGet("/trash", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(20) <= 0 ? 20 : Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var query = db.Comments
                .AsNoTracking()
                .Where(c => c.IsDeleted)
                .Include(c => c.Post)
                .Include(c => c.Author)
                .AsQueryable();

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var items = await query
                .OrderByDescending(c => c.DeletedAt ?? c.UpdatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(c => new TrashCommentItemDto(
                    c.Id,
                    c.PostId,
                    c.Post != null ? c.Post.Title : string.Empty,
                    c.Content,
                    c.Status,
                    c.IsGuest,
                    c.DeletedAt,
                    c.CreatedAt,
                    new CommentAuthorDto(
                        c.Author.Id,
                        c.Author.Username,
                        c.Author.DisplayName ?? "Anonymous",
                        c.Author.AvatarUrl
                    )
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<TrashCommentItemDto>(items, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("AdminGetTrashComments")
        .WithSummary("List all soft-deleted comments in the Trash bin (Admin Only)");

        return adminCommentsGroup;
    }
}
