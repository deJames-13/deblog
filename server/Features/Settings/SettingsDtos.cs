namespace deblog.Server.Features.Settings;

public record SiteSettingsResponse(
    Guid UserId,
    string Email,
    string Username,
    string DisplayName,
    string? Role,
    string? Tagline,
    string? Bio,
    string? Location,
    string? AvatarUrl,
    string? BannerUrl,
    string? CopyrightYear,
    string? SocialLinksJson,
    bool CloudinaryConfigured
);

public record UpdateSiteSettingsRequest(
    string? DisplayName,
    string? Role,
    string? Tagline,
    string? Bio,
    string? Location,
    string? AvatarUrl,
    string? BannerUrl,
    string? CopyrightYear,
    string? SocialLinksJson
);

public record UploadSettingAssetResponse(
    string Url,
    string PublicId,
    string Message
);
