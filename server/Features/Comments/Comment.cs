using deblog.Server.Common.Entities;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Users;

namespace deblog.Server.Features.Comments;

public enum CommentStatus
{
    Pending = 0,
    Approved = 1,
    Removed = 2
}

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    public Guid ManagementToken { get; set; } = Guid.NewGuid();
    public bool IsGuest { get; set; } = true;

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
}
