using System.ComponentModel.DataAnnotations.Schema;
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
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    [NotMapped]
    public bool IsPublished => Status == PostStatus.Published;

    public string? CoverImageUrl { get; set; }
    public string? Category { get; set; }
    public string[] Tags { get; set; } = [];
    public bool IsFeatured { get; set; } = false;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public PostAnalytics? Analytics { get; set; }
}
