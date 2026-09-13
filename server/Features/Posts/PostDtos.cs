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

[method: JsonConstructor]
public record PostListItemDto(
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
    AuthorSummaryDto Author,
    PostAnalyticsDto Analytics,
    string? CoverImageUrl = null,
    string? Category = null,
    string[]? Tags = null,
    bool IsFeatured = false
)
{
    public PostListItemDto(
        Guid id, string title, string slug, string? summary, string url,
        PostStatus status, bool isPublished, bool isDeleted, DateTime? deletedAt,
        DateTime? publishedAt, DateTime createdAt, AuthorSummaryDto author, PostAnalyticsDto analytics)
        : this(id, title, slug, summary, string.Empty, url, status, isPublished, isDeleted, deletedAt, publishedAt, createdAt, author, analytics, null, null, null, false)
    {
    }

    public PostListItemDto(
        Guid id, string title, string slug, string? summary, string content, string url,
        PostStatus status, bool isPublished, bool isDeleted, DateTime? deletedAt,
        DateTime? publishedAt, DateTime createdAt, AuthorSummaryDto author, PostAnalyticsDto analytics)
        : this(id, title, slug, summary, content, url, status, isPublished, isDeleted, deletedAt, publishedAt, createdAt, author, analytics, null, null, null, false)
    {
    }
}

[method: JsonConstructor]
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
    PostAnalyticsDto Analytics,
    string? CoverImageUrl = null,
    string? Category = null,
    string[]? Tags = null,
    bool IsFeatured = false
)
{
    public PostDetailDto(
        Guid id, string title, string slug, string? summary, string content, string url,
        PostStatus status, bool isPublished, bool isDeleted, DateTime? deletedAt,
        DateTime? publishedAt, DateTime createdAt, DateTime updatedAt,
        AuthorSummaryDto author, PostAnalyticsDto analytics)
        : this(id, title, slug, summary, content, url, status, isPublished, isDeleted, deletedAt, publishedAt, createdAt, updatedAt, author, analytics, null, null, null, false)
    {
    }
}

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
    bool? IsPublished = null,
    string? CoverImageUrl = null,
    string? Category = null,
    string[]? Tags = null,
    bool IsFeatured = false
)
{
    public CreatePostRequest(string title, string? slug, string? summary, string content, bool isPublished)
        : this(title, slug, summary, content, isPublished ? PostStatus.Published : PostStatus.Draft, isPublished, null, null, null, false)
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
    bool? IsPublished = null,
    string? CoverImageUrl = null,
    string? Category = null,
    string[]? Tags = null,
    bool? IsFeatured = null
)
{
    public UpdatePostRequest(string? title, string? slug, string? summary, string? content, bool isPublished)
        : this(title, slug, summary, content, isPublished ? PostStatus.Published : PostStatus.Draft, isPublished, null, null, null, null)
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
