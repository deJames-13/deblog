using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Features.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // ==================== User Profile Endpoints ====================

        var group = app.MapGroup("/api/users")
            .WithTags("Users");

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
                    Role = isAdminEmail ? UserRoles.Admin : UserRoles.User
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

            var dto = new UserProfileDto(
                existingUser.Id,
                existingUser.Email,
                existingUser.Username,
                existingUser.DisplayName,
                existingUser.Bio,
                existingUser.AvatarUrl,
                existingUser.Role,
                existingUser.CreatedAt
            );

            return Results.Ok(dto);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get or sync the current authenticated user's profile");

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

            if (request.DisplayName != null) existingUser.DisplayName = request.DisplayName;
            if (request.Bio != null) existingUser.Bio = request.Bio;
            if (request.AvatarUrl != null) existingUser.AvatarUrl = request.AvatarUrl;

            await db.SaveChangesAsync(ct);

            var dto = new UserProfileDto(
                existingUser.Id,
                existingUser.Email,
                existingUser.Username,
                existingUser.DisplayName,
                existingUser.Bio,
                existingUser.AvatarUrl,
                existingUser.Role,
                existingUser.CreatedAt
            );

            return Results.Ok(dto);
        })
        .RequireAuthorization()
        .WithName("UpdateCurrentUser")
        .WithSummary("Update current authenticated user's profile");

        group.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, ct);

            if (user == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            var dto = new UserProfileDto(
                user.Id,
                user.Email,
                user.Username,
                user.DisplayName,
                user.Bio,
                user.AvatarUrl,
                user.Role,
                user.CreatedAt
            );

            return Results.Ok(dto);
        })
        .WithName("GetUserById")
        .WithSummary("Get public user profile by ID");

        // ==================== Admin User Management CRUD ====================

        var adminGroup = app.MapGroup("/api/admin/users")
            .WithTags("Admin Users")
            .RequireAuthorization("AdminOnly");

        // GET /api/admin/users - List users with pagination and filtering
        adminGroup.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? role,
            [FromQuery] string? search,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(20) <= 0 ? 20 : Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var query = db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role.ToLower() == role.Trim().ToLower());
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
                .Select(u => new UserProfileDto(
                    u.Id,
                    u.Email,
                    u.Username,
                    u.DisplayName,
                    u.Bio,
                    u.AvatarUrl,
                    u.Role,
                    u.CreatedAt
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<UserProfileDto>(users, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("AdminListUsers")
        .WithSummary("List all users (Admin Only)");

        // POST /api/admin/users - Admin create user
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
                Role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role.Trim() : UserRoles.User
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct);

            var dto = new UserProfileDto(
                newUser.Id,
                newUser.Email,
                newUser.Username,
                newUser.DisplayName,
                newUser.Bio,
                newUser.AvatarUrl,
                newUser.Role,
                newUser.CreatedAt
            );

            return Results.Created($"/api/users/{newUser.Id}", dto);
        })
        .WithName("AdminCreateUser")
        .WithSummary("Create a new user account (Admin Only)");

        // PUT /api/admin/users/{id:guid} - Admin update user
        adminGroup.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] AdminUpdateUserRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
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

            await db.SaveChangesAsync(ct);

            var dto = new UserProfileDto(
                existingUser.Id,
                existingUser.Email,
                existingUser.Username,
                existingUser.DisplayName,
                existingUser.Bio,
                existingUser.AvatarUrl,
                existingUser.Role,
                existingUser.CreatedAt
            );

            return Results.Ok(dto);
        })
        .WithName("AdminUpdateUser")
        .WithSummary("Update user details or role (Admin Only)");

        // DELETE /api/admin/users/{id:guid} - Admin delete user
        adminGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal currentUser,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var userToDelete = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (userToDelete == null)
            {
                return Results.NotFound(new { message = "User not found" });
            }

            var currentUserId = currentUser.GetUserId();
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                return Results.BadRequest(new { message = "You cannot delete your own admin account" });
            }

            db.Users.Remove(userToDelete);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("AdminDeleteUser")
        .WithSummary("Delete a user account (Admin Only)");

        return app;
    }
}
