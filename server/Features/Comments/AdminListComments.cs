using deblog.Server.Common.Data;
using deblog.Server.Features.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class AdminListCommentsEndpoint
{
    public static RouteGroupBuilder MapAdminListComments(this RouteGroupBuilder adminCommentsGroup)
    {
        adminCommentsGroup.MapGet("/", async (
            [FromQuery] CommentStatus? status,
            [FromQuery] Guid? postId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(20) <= 0 ? 20 : Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var query = db.Comments
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Include(c => c.Post)
                .Include(c => c.Author)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (postId.HasValue)
            {
                query = query.Where(c => c.PostId == postId.Value);
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(c => CommentHelpers.ToAdminResponseDto(c))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<AdminCommentResponseDto>(items, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("AdminListComments")
        .WithSummary("List all active comments for moderation (Admin Only)");

        return adminCommentsGroup;
    }
}
