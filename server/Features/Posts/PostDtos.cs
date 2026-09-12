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
    bool IsPublished,
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
    bool IsPublished,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorSummaryDto Author,
    PostAnalyticsDto Analytics
);

public record CreatePostRequest(
    string Title,
    string? Slug,
    string? Summary,
    string Content,
    bool IsPublished = false
);

public record UpdatePostRequest(
    string? Title,
    string? Slug,
    string? Summary,
    string? Content,
    bool? IsPublished
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
