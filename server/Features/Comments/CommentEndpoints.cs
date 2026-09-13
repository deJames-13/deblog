namespace deblog.Server.Features.Comments;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        // Public & Guest post comments: /api/posts/{postId:guid}/comments
        var postCommentsGroup = app.MapGroup("/api/posts/{postId:guid}/comments")
            .WithTags("Comments");

        postCommentsGroup.MapGetApprovedPostComments();
        postCommentsGroup.MapCreateGuestComment();

        // Guest Token & Admin comment actions: /api/comments
        var commentsGroup = app.MapGroup("/api/comments")
            .WithTags("Comments");

        commentsGroup.MapUpdateComment();
        commentsGroup.MapSoftDeleteComment();

        // Admin moderation group: /api/admin/comments
        var adminCommentsGroup = app.MapGroup("/api/admin/comments")
            .WithTags("Admin Comments")
            .RequireAuthorization("AdminOnly");

        adminCommentsGroup.MapAdminListComments();
        adminCommentsGroup.MapAdminGetTrashComments();
        adminCommentsGroup.MapAdminUpdateCommentStatus();
        adminCommentsGroup.MapAdminRestoreComment();
        adminCommentsGroup.MapAdminForceDeleteComment();

        return app;
    }
}
