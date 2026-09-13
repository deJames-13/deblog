namespace deblog.Server.Features.Comments;

public static class CommentHelpers
{
    public static CommentResponseDto ToResponseDto(Comment c)
    {
        return new CommentResponseDto(
            c.Id,
            c.PostId,
            c.Content,
            c.Status,
            c.IsDeleted,
            c.DeletedAt,
            c.CreatedAt,
            c.UpdatedAt,
            new CommentAuthorDto(
                c.Author?.Id ?? Guid.Empty,
                c.Author?.Username ?? "anonymous",
                c.Author?.DisplayName ?? "Anonymous",
                c.Author?.AvatarUrl
            )
        );
    }

    public static AdminCommentResponseDto ToAdminResponseDto(Comment c)
    {
        return new AdminCommentResponseDto(
            c.Id,
            c.PostId,
            c.Post != null ? c.Post.Title : string.Empty,
            c.Content,
            c.Status,
            c.IsGuest,
            c.IsDeleted,
            c.DeletedAt,
            c.CreatedAt,
            c.UpdatedAt,
            new CommentAuthorDto(
                c.Author?.Id ?? Guid.Empty,
                c.Author?.Username ?? "anonymous",
                c.Author?.DisplayName ?? "Anonymous",
                c.Author?.AvatarUrl
            )
        );
    }
}
