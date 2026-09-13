using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminRestoreUserEndpoint
{
    public static RouteGroupBuilder MapAdminRestoreUser(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapPost("/{id:guid}/restore", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found in trash" });
            }

            user.IsDeleted = false;
            user.DeletedAt = null;
            await db.SaveChangesAsync(ct);

            return Results.Ok(UserHelpers.ToProfileDto(user));
        })
        .WithName("AdminRestoreUser")
        .WithSummary("Restore a soft-deleted user account from trash (Admin Only)");

        return adminGroup;
    }
}
