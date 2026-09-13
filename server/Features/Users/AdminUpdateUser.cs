using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminUpdateUserEndpoint
{
    public static RouteGroupBuilder MapAdminUpdateUser(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] AdminUpdateUserRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
            if (existingUser == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.ToLower() != existingUser.Email.ToLower())
            {
                var emailTaken = await db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id, ct);
                if (emailTaken) return Results.BadRequest(new { message = "Email is already taken" });
                existingUser.Email = request.Email.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(request.Username) && request.Username.ToLower() != existingUser.Username.ToLower())
            {
                var usernameTaken = await db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower() && u.Id != id, ct);
                if (usernameTaken) return Results.BadRequest(new { message = "Username is already taken" });
                existingUser.Username = request.Username.Trim();
            }

            if (request.DisplayName != null) existingUser.DisplayName = request.DisplayName.Trim();
            if (request.Bio != null) existingUser.Bio = request.Bio.Trim();
            if (request.AvatarUrl != null) existingUser.AvatarUrl = request.AvatarUrl.Trim();
            if (!string.IsNullOrWhiteSpace(request.Role)) existingUser.Role = request.Role.Trim();
            if (request.Status.HasValue) existingUser.Status = request.Status.Value;

            await db.SaveChangesAsync(ct);

            return Results.Ok(UserHelpers.ToProfileDto(existingUser));
        })
        .WithName("AdminUpdateUser")
        .WithSummary("Update user details or role (Admin Only)");

        return adminGroup;
    }
}
