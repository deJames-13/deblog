using deblog.Server.Common.Entities;

namespace deblog.Server.Features.Users;

public class UserInformation : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? JobTitle { get; set; }
    public string? Tagline { get; set; }
    public string? Location { get; set; }
    public string? BannerUrl { get; set; }
    public string? CopyrightYear { get; set; }
    public string? SocialLinksJson { get; set; }
}
