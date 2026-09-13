using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class AdminCreateUserEndpoint
{
    public static RouteGroupBuilder MapAdminCreateUser(this RouteGroupBuilder adminGroup)
    {
        adminGroup.MapPost("/", async (
            [FromBody] AdminCreateUserRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { message = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest(new { message = "Username is required" });
            }

            var emailExists = await db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), ct);
            if (emailExists)
            {
                return Results.BadRequest(new { message = "A user with this email already exists" });
            }

            var usernameExists = await db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower(), ct);
            if (usernameExists)
            {
                return Results.BadRequest(new { message = "Username is already taken" });
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Username = request.Username.Trim(),
                DisplayName = request.DisplayName?.Trim() ?? request.Username.Trim(),
                Bio = request.Bio?.Trim(),
                AvatarUrl = request.AvatarUrl?.Trim(),
                Role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role.Trim() : UserRoles.User,
                Status = request.Status,
                IsDeleted = false
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/users/{newUser.Id}", UserHelpers.ToProfileDto(newUser));
        })
        .WithName("AdminCreateUser")
        .WithSummary("Create a new user account with initial status (Admin Only)");

        return adminGroup;
    }
}
