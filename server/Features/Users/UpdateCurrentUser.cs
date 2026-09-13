using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class UpdateCurrentUserEndpoint
{
    public static RouteGroupBuilder MapUpdateCurrentUser(this RouteGroupBuilder group)
    {
        group.MapPut("/me", async (
            ClaimsPrincipal user,
            [FromBody] UpdateUserProfileRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var email = user.GetUserEmail() ?? string.Empty;
            var existingUser = await db.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value || (!string.IsNullOrWhiteSpace(email) && u.Email.ToLower() == email.ToLower()), ct);

            if (existingUser == null)
            {
                return Results.NotFound(new { message = "User profile not found. Request GET /api/users/me first." });
            }

            // Check account status
            if (existingUser.IsDeleted)
            {
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Account Deactivated", detail: "This account has been deactivated.");
            }

            if (existingUser.Status == UserStatus.Suspended)
            {
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Account Suspended", detail: "This account is currently suspended.");
            }

            if (existingUser.Status == UserStatus.Banned)
            {
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Account Banned", detail: "This account has been banned.");
            }

            if (request.DisplayName != null) existingUser.DisplayName = request.DisplayName.Trim();
            if (request.Bio != null) existingUser.Bio = request.Bio.Trim();
            if (request.AvatarUrl != null) existingUser.AvatarUrl = request.AvatarUrl.Trim();

            await db.SaveChangesAsync(ct);

            return Results.Ok(UserHelpers.ToProfileDto(existingUser));
        })
        .RequireAuthorization()
        .WithName("UpdateCurrentUser")
        .WithSummary("Update current authenticated user's profile");

        return group;
    }
}
