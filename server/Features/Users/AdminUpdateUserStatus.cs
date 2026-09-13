using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminUpdateUserStatusEndpoint
{
    public static RouteGroupBuilder MapAdminUpdateUserStatus(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateUserStatusRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            user.Status = request.Status;
            await db.SaveChangesAsync(ct);

            return Results.Ok(UserHelpers.ToProfileDto(user));
        })
        .WithName("AdminUpdateUserStatus")
        .WithSummary("Transition user status between Active, Suspended, and Banned (Admin Only)");

        return adminGroup;
    }
}
