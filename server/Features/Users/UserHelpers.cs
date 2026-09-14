namespace deblog.Server.Features.Users;

public static class UserHelpers
{
    public static UserProfileDto ToProfileDto(User user)
    {
        var infoDto = user.Information != null
            ? new UserInformationDto(
                user.Information.JobTitle,
                user.Information.Tagline,
                user.Information.Location,
                user.Information.BannerUrl,
                user.Information.CopyrightYear,
                user.Information.SocialLinksJson)
            : null;

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
            user.CreatedAt,
            infoDto
        );
    }
}
