using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class GetUserByIdEndpoint
{
    public static RouteGroupBuilder MapGetUserById(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            return Results.Ok(UserHelpers.ToProfileDto(user));
        })
        .WithName("GetUserById")
        .WithSummary("Get public user profile by ID");

        return group;
    }
}
