namespace deblog.Server.Features.Posts;

public static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts")
            .WithTags("Posts");

        group.MapGetPosts();
        group.MapGetTrashPosts();
        group.MapGetPostByIdOrSlug();
        group.MapCreatePost();
        group.MapUpdatePost();
        group.MapUpdatePostStatus();
        group.MapHidePost();
        group.MapArchivePost();
        group.MapSoftDeletePost();
        group.MapRestorePost();
        group.MapForceDeletePost();
        group.MapTrackPostAnalytics();

        return app;
    }
}
