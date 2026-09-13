using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminForceDeleteUserEndpoint
{
    public static RouteGroupBuilder MapAdminForceDeleteUser(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapDelete("/{id:guid}/force", async (
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

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("AdminForceDeleteUser")
        .WithSummary("Permanently delete a user account from database (Admin Only)");

        return adminGroup;
    }
}
