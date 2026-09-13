using deblog.Server.Common.Entities;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Posts;

namespace deblog.Server.Features.Users;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Guest = "Guest";
}

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = UserRoles.Guest;

    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
