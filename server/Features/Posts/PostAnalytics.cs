using deblog.Server.Common.Entities;

namespace deblog.Server.Features.Posts;

public class PostAnalytics : BaseEntity
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int Views { get; set; } = 0;
    public int Likes { get; set; } = 0;
    public int Shares { get; set; } = 0;
}
