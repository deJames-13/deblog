using System.Text.Json.Serialization;

namespace deblog.Server.Features.Comments;

public record CommentAuthorDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? AvatarUrl
);

[method: JsonConstructor]
public record CommentResponseDto(
    Guid Id,
    Guid PostId,
    string Content,
    CommentStatus Status,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    CommentAuthorDto Author
)
{
    public CommentResponseDto(
        Guid id,
        Guid postId,
        string content,
        CommentStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        CommentAuthorDto author)
        : this(id, postId, content, status, false, null, createdAt, updatedAt, author)
    {
    }
}

public record CommentCreatedResponseDto(
    Guid Id,
    Guid PostId,
    string Content,
    CommentStatus Status,
    Guid ManagementToken,
    DateTime CreatedAt,
    CommentAuthorDto Author
);

[method: JsonConstructor]
public record AdminCommentResponseDto(
    Guid Id,
    Guid PostId,
    string PostTitle,
    string Content,
    CommentStatus Status,
    bool IsGuest,
    bool IsDeleted,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    CommentAuthorDto Author
)
{
    public AdminCommentResponseDto(
        Guid id,
        Guid postId,
        string postTitle,
        string content,
        CommentStatus status,
        bool isGuest,
        DateTime createdAt,
        DateTime updatedAt,
        CommentAuthorDto author)
        : this(id, postId, postTitle, content, status, isGuest, false, null, createdAt, updatedAt, author)
    {
    }
}

public record TrashCommentItemDto(
    Guid Id,
    Guid PostId,
    string PostTitle,
    string Content,
    CommentStatus Status,
    bool IsGuest,
    DateTime? DeletedAt,
    DateTime CreatedAt,
    CommentAuthorDto Author
);

public record CreateGuestCommentRequest(
    string Content,
    string Email,
    string? DisplayName
);

public record UpdateCommentRequest(
    string Content,
    Guid? ManagementToken
);

public record UpdateCommentStatusRequest(
    CommentStatus Status
);
