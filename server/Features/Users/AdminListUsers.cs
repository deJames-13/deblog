using deblog.Server.Common.Data;
using deblog.Server.Features.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminListUsersEndpoint
{
    public static RouteGroupBuilder MapAdminListUsers(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? role,
            [FromQuery] UserStatus? status,
            [FromQuery] string? search,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(20) <= 0 ? 20 : Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var query = db.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role.ToLower() == role.Trim().ToLower());
            }

            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

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
                .OrderByDescending(u => u.CreatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(u => UserHelpers.ToProfileDto(u))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<UserProfileDto>(users, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("AdminListUsers")
        .WithSummary("List all active users with pagination, role, and status filtering (Admin Only)");

        return adminGroup;
    }
}
