using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class GetCurrentUserEndpoint
{
    public static RouteGroupBuilder MapGetCurrentUser(this RouteGroupBuilder group)
    {
        group.MapGet("/me", async (
            ClaimsPrincipal user,
            IConfiguration config,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var email = user.GetUserEmail() ?? string.Empty;
            var isAdminEmail = !string.IsNullOrWhiteSpace(email) &&
                string.Equals(email, config["AUTHOR_EMAIL"] ?? config["ADMIN_EMAIL"], StringComparison.OrdinalIgnoreCase);

            var existingUser = await db.Users
                .Include(u => u.Information)
                .FirstOrDefaultAsync(u => u.Id == userId.Value || (!string.IsNullOrWhiteSpace(email) && u.Email.ToLower() == email.ToLower()), ct);

            if (existingUser == null)
            {
                var defaultUsername = !string.IsNullOrWhiteSpace(email)
                    ? email.Split('@')[0]
                    : $"user_{userId.Value.ToString()[..8]}";

                var count = await db.Users.CountAsync(u => u.Username.StartsWith(defaultUsername), ct);
                if (count > 0)
                {
                    defaultUsername = $"{defaultUsername}_{count + 1}";
                }

                existingUser = new User
                {
                    Id = userId.Value,
                    Email = email,
                    Username = defaultUsername,
                    DisplayName = defaultUsername,
                    Role = isAdminEmail ? UserRoles.Admin : UserRoles.User,
                    Status = UserStatus.Active,
                    IsDeleted = false
                };

                db.Users.Add(existingUser);
                await db.SaveChangesAsync(ct);
            }
            else if (existingUser.Id != userId.Value)
            {
                // Sync Supabase Auth UID with existing record
                existingUser.Id = userId.Value;
                if (isAdminEmail) existingUser.Role = UserRoles.Admin;
                await db.SaveChangesAsync(ct);
            }
            else if (isAdminEmail && existingUser.Role != UserRoles.Admin)
            {
                existingUser.Role = UserRoles.Admin;
                await db.SaveChangesAsync(ct);
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

            return Results.Ok(UserHelpers.ToProfileDto(existingUser));
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get or sync the current authenticated user's profile");

        return group;
    }
}
