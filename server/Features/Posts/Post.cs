using deblog.Server.Common.Entities;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Users;

namespace deblog.Server.Features.Posts;

public class Post : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public PostAnalytics? Analytics { get; set; }
}

