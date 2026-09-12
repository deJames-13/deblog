namespace deblog.Server.Features.Comments;

public record CommentAuthorDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? AvatarUrl
);

public record CommentResponseDto(
    Guid Id,
    Guid PostId,
    string Content,
    CommentStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    CommentAuthorDto Author
);

public record CommentCreatedResponseDto(
    Guid Id,
    Guid PostId,
    string Content,
    CommentStatus Status,
    Guid ManagementToken,
    DateTime CreatedAt,
    CommentAuthorDto Author
);

public record AdminCommentResponseDto(
    Guid Id,
    Guid PostId,
    string PostTitle,
    string Content,
    CommentStatus Status,
    bool IsGuest,
    DateTime CreatedAt,
    DateTime UpdatedAt,
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
