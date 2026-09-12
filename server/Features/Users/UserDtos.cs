namespace deblog.Server.Features.Users;

public record UserProfileDto(
    Guid Id,
    string Email,
    string Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string Role,
    DateTime CreatedAt
);

public record UpdateUserProfileRequest(
    string? DisplayName,
    string? Bio,
    string? AvatarUrl
);

public record AdminCreateUserRequest(
    string Email,
    string Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Role
);

public record AdminUpdateUserRequest(
    string? Email,
    string? Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Role
);
