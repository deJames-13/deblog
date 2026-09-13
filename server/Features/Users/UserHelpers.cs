namespace deblog.Server.Features.Users;

public static class UserHelpers
{
    public static UserProfileDto ToProfileDto(User user)
    {
        return new UserProfileDto(
            user.Id,
            user.Email,
            user.Username,
            user.DisplayName,
            user.Bio,
            user.AvatarUrl,
            user.Role,
            user.Status,
            user.IsDeleted,
            user.DeletedAt,
            user.CreatedAt
        );
    }
}
