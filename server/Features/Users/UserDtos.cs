using System.Text.Json.Serialization;

namespace deblog.Server.Features.Users;

public record UserInformationDto(
    string? JobTitle,
    string? Tagline,
    string? Location,
    string? BannerUrl,
    string? CopyrightYear,
    string? SocialLinksJson
);

[method: JsonConstructor]
public record UserProfileDto(
    Guid Id,
    string Email,
    string Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string Role,
    UserStatus Status,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    UserInformationDto? Information = null
)
{
    // Convenience constructor for backward compatibility with 8 arguments
    public UserProfileDto(
        Guid id,
        string email,
        string username,
        string? displayName,
        string? bio,
        string? avatarUrl,
        string role,
        DateTime createdAt)
        : this(id, email, username, displayName, bio, avatarUrl, role, UserStatus.Active, false, null, createdAt, null)
    {
    }
}

public record TrashUserItemDto(
    Guid Id,
    string Email,
    string Username,
    string? DisplayName,
    string Role,
    UserStatus Status,
    DateTime? DeletedAt,
    DateTime CreatedAt
);

public record UpdateUserProfileRequest(
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? JobTitle = null,
    string? Tagline = null,
    string? Location = null,
    string? BannerUrl = null,
    string? CopyrightYear = null,
    string? SocialLinksJson = null
);

[method: JsonConstructor]
public record AdminCreateUserRequest(
    string Email,
    string Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Role,
    UserStatus Status = UserStatus.Active
)
{
    public AdminCreateUserRequest(string email, string username, string? displayName, string? bio, string? avatarUrl, string? role)
        : this(email, username, displayName, bio, avatarUrl, role, UserStatus.Active)
    {
    }
}

[method: JsonConstructor]
public record AdminUpdateUserRequest(
    string? Email,
    string? Username,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Role,
    UserStatus? Status = null
)
{
    public AdminUpdateUserRequest(string? email, string? username, string? displayName, string? bio, string? avatarUrl, string? role)
        : this(email, username, displayName, bio, avatarUrl, role, null)
    {
    }
}

public record UpdateUserStatusRequest(
    UserStatus Status
);
