using deblog.Server.Common.Data;
using deblog.Server.Features.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminGetTrashUsersEndpoint
{
    public static RouteGroupBuilder MapAdminGetTrashUsers(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapGet("/trash", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(20) <= 0 ? 20 : Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var query = db.Users
                .AsNoTracking()
                .Where(u => u.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u => u.Email.ToLower().Contains(term) ||
                                         u.Username.ToLower().Contains(term) ||
                                         (u.DisplayName != null && u.DisplayName.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var users = await query
                .OrderByDescending(u => u.DeletedAt ?? u.UpdatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(u => new TrashUserItemDto(
                    u.Id,
                    u.Email,
                    u.Username,
                    u.DisplayName,
                    u.Role,
                    u.Status,
                    u.DeletedAt,
                    u.CreatedAt
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<TrashUserItemDto>(users, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("AdminGetTrashUsers")
        .WithSummary("List all soft-deleted users in the Trash bin (Admin Only)");

        return adminGroup;
    }
}
