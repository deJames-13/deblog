using System.Text.Json.Serialization;

namespace deblog.Server.Features.Posts;

public record AuthorSummaryDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? AvatarUrl
);

public record PostAnalyticsDto(
    int Views,
    int Likes,
    int Shares,
    int CommentsCount
);

public record PostListItemDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string Url,
    PostStatus Status,
    bool IsPublished,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    AuthorSummaryDto Author,
    PostAnalyticsDto Analytics
);

public record PostDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string Url,
    PostStatus Status,
    bool IsPublished,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorSummaryDto Author,
    PostAnalyticsDto Analytics
);

public record TrashPostItemDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    PostStatus Status,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    AuthorSummaryDto Author
);

[method: JsonConstructor]
public record CreatePostRequest(
    string Title,
    string? Slug = null,
    string? Summary = null,
    string Content = "",
    PostStatus Status = PostStatus.Draft,
    bool? IsPublished = null
)
{
    public CreatePostRequest(string title, string? slug, string? summary, string content, bool isPublished)
        : this(title, slug, summary, content, isPublished ? PostStatus.Published : PostStatus.Draft, isPublished)
    {
    }
}

[method: JsonConstructor]
public record UpdatePostRequest(
    string? Title = null,
    string? Slug = null,
    string? Summary = null,
    string? Content = null,
    PostStatus? Status = null,
    bool? IsPublished = null
)
{
    public UpdatePostRequest(string? title, string? slug, string? summary, string? content, bool isPublished)
        : this(title, slug, summary, content, isPublished ? PostStatus.Published : PostStatus.Draft, isPublished)
    {
    }
}

public record UpdatePostStatusRequest(
    PostStatus Status
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
