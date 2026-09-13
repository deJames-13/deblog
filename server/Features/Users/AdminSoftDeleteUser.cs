using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminSoftDeleteUserEndpoint
{
    public static RouteGroupBuilder MapAdminSoftDeleteUser(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentUserId = currentUser.GetUserId();
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                return Results.BadRequest(new { message = "You cannot delete your own admin account" });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("AdminSoftDeleteUser")
        .WithSummary("Move a user account to trash / soft-delete (Admin Only)");

        return adminGroup;
    }
}
