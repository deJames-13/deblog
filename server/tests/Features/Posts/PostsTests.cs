using System.Net;
using System.Net.Http.Json;
using deblog.Server.Features.Posts;
using deblog.Server.Tests.Common;

namespace deblog.Server.Tests.Features.Posts;

public class PostsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PostsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousClient_AccessingPostCreation_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();
        var request = new CreatePostRequest("Unauthorized Post", null, "Summary", "Content", true);

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegularUserClient_AccessingPostCreation_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateUserClient("reader@example.com", "User");
        var request = new CreatePostRequest("Forbidden Post", null, "Summary", "Content", true);

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreatePost_WithCanonicalUrlAndAnalytics()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var title = $"Test Post {Guid.NewGuid().ToString()[..6]}";
        var request = new CreatePostRequest(title, null, "Test Summary", "## Markdown Content", true);

        // Act
        var createResponse = await adminClient.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>();

        Assert.NotNull(post);
        Assert.Equal(title, post.Title);
        Assert.True(post.IsPublished);
        Assert.NotNull(post.Url);
        Assert.Contains(post.Slug, post.Url);
        Assert.NotNull(post.Analytics);
        Assert.Equal(0, post.Analytics.Views);
    }

    [Theory]
    [InlineData("Draft", PostStatus.Draft)]
    [InlineData("draft", PostStatus.Draft)]
    [InlineData("Published", PostStatus.Published)]
    [InlineData("published", PostStatus.Published)]
    public async Task Admin_CanCreatePost_WithStringStatus_Draft_Or_Published(string statusString, PostStatus expectedStatus)
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var title = $"String Status Post {Guid.NewGuid().ToString()[..6]}";
        var json = $$"""
        {
            "title": "{{title}}",
            "slug": "{{title.ToLowerInvariant().Replace(' ', '-')}}",
            "summary": "Summary with string status",
            "content": "Content with string status",
            "status": "{{statusString}}",
            "isFeatured": false
        }
        """;
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await adminClient.PostAsync("/api/posts", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await response.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);
        Assert.Equal(expectedStatus, post.Status);
    }

    [Fact]
    public async Task AnonymousClient_CanViewPublishedPosts_AndTrackViews()
    {
        // Arrange - Admin creates published post
        var adminClient = _factory.CreateAdminClient();
        var title = $"Public Post {Guid.NewGuid().ToString()[..6]}";
        var createResponse = await adminClient.PostAsJsonAsync("/api/posts",
            new CreatePostRequest(title, null, "Summary", "Content", true));
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);

        var anonClient = _factory.CreateAnonymousClient();

        // Act 1 - Fetch published post by slug
        var getResponse = await anonClient.GetAsync($"/api/posts/{post.Slug}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // Act 2 - Increment View counter
        var viewResponse1 = await anonClient.PostAsync($"/api/posts/{post.Slug}/analytics/view", null);
        Assert.Equal(HttpStatusCode.OK, viewResponse1.StatusCode);

        // Act 3 - Repeat view from same client should be deduplicated (cooldown active)
        var viewResponse2 = await anonClient.PostAsync($"/api/posts/{post.Slug}/analytics/view", null);
        Assert.Equal(HttpStatusCode.OK, viewResponse2.StatusCode);

        // Act 4 - Track like
        var likeResponse = await anonClient.PostAsync($"/api/posts/{post.Slug}/analytics/like", null);
        Assert.Equal(HttpStatusCode.OK, likeResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CanUpdateAndDeletePost()
    {
        // Arrange - Create a post
        var adminClient = _factory.CreateAdminClient();
        var createResponse = await adminClient.PostAsJsonAsync("/api/posts",
            new CreatePostRequest("Post to Edit", null, "Old Summary", "Old Content", true));
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);

        // Act 1 - Update
        var updateResponse = await adminClient.PutAsJsonAsync($"/api/posts/{post.Id}",
            new UpdatePostRequest("Updated Title", null, "New Summary", "New Content", true));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.Equal("Updated Title", updated!.Title);

        // Act 2 - Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Act 3 - Verify deleted
        var getDeleted = await adminClient.GetAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreatePost_AsDraft_AndPublicCannotSeeIt()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var title = $"Draft Post {Guid.NewGuid().ToString()[..6]}";
        var createRequest = new CreatePostRequest(title, null, "Draft Summary", "Draft Content", PostStatus.Draft);

        // Act 1 - Admin creates Draft post
        var createResponse = await adminClient.PostAsJsonAsync("/api/posts", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);
        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.False(post.IsPublished);

        // Act 2 - Anonymous user cannot view the draft post by slug
        var anonClient = _factory.CreateAnonymousClient();
        var anonGet = await anonClient.GetAsync($"/api/posts/{post.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, anonGet.StatusCode);

        // Act 3 - Anonymous user does not see it in the public list
        var anonList = await anonClient.GetFromJsonAsync<PagedResult<PostListItemDto>>("/api/posts");
        Assert.NotNull(anonList);
        Assert.DoesNotContain(anonList.Items, p => p.Id == post.Id);

        // Act 4 - Admin CAN view the draft post
        var adminGet = await adminClient.GetAsync($"/api/posts/{post.Slug}");
        Assert.Equal(HttpStatusCode.OK, adminGet.StatusCode);

        // Act 5 - Admin CAN filter list by Draft status
        var adminList = await adminClient.GetFromJsonAsync<PagedResult<PostListItemDto>>("/api/posts?status=0");
        Assert.NotNull(adminList);
        Assert.Contains(adminList.Items, p => p.Id == post.Id);
    }

    [Fact]
    public async Task Admin_CanTransitionPostStatus_Draft_To_Published_To_Hidden_To_Archived()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var anonClient = _factory.CreateAnonymousClient();
        var title = $"Lifecycle Post {Guid.NewGuid().ToString()[..6]}";
        var createResponse = await adminClient.PostAsJsonAsync("/api/posts",
            new CreatePostRequest(title, null, "Summary", "Content", PostStatus.Draft));
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);

        // Transition: Draft -> Published via PATCH /api/posts/{id}/status
        var publishResponse = await adminClient.PatchAsJsonAsync($"/api/posts/{post.Id}/status",
            new UpdatePostStatusRequest(PostStatus.Published));
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var publishedPost = await publishResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.Equal(PostStatus.Published, publishedPost!.Status);
        Assert.True(publishedPost.IsPublished);
        Assert.NotNull(publishedPost.PublishedAt);

        // Public visitor can now view it
        var publicGet = await anonClient.GetAsync($"/api/posts/{post.Slug}");
        Assert.Equal(HttpStatusCode.OK, publicGet.StatusCode);

        // Transition: Published -> Hidden via POST /api/posts/{id}/hide
        var hideResponse = await adminClient.PostAsync($"/api/posts/{post.Id}/hide", null);
        Assert.Equal(HttpStatusCode.OK, hideResponse.StatusCode);
        var hiddenPost = await hideResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.Equal(PostStatus.Hidden, hiddenPost!.Status);

        // Public visitor can no longer view it
        var hiddenPublicGet = await anonClient.GetAsync($"/api/posts/{post.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenPublicGet.StatusCode);

        // Transition: Hidden -> Archived via POST /api/posts/{id}/archive
        var archiveResponse = await adminClient.PostAsync($"/api/posts/{post.Id}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        var archivedPost = await archiveResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.Equal(PostStatus.Archived, archivedPost!.Status);

        // Public visitor still cannot view it
        var archivedPublicGet = await anonClient.GetAsync($"/api/posts/{post.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, archivedPublicGet.StatusCode);
    }

    [Fact]
    public async Task Admin_CanSoftDelete_ListInTrash_Restore_AndForceDeletePost()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var anonClient = _factory.CreateAnonymousClient();
        var title = $"Trash Lifecycle Post {Guid.NewGuid().ToString()[..6]}";
        var createResponse = await adminClient.PostAsJsonAsync("/api/posts",
            new CreatePostRequest(title, null, "Summary", "Content", PostStatus.Published));
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(post);

        // Act 1 - Soft Delete via DELETE /api/posts/{id}
        var softDeleteResponse = await adminClient.DeleteAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NoContent, softDeleteResponse.StatusCode);

        // Act 2 - Verify it is excluded from normal queries (admin & public)
        var getPost = await adminClient.GetAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getPost.StatusCode);

        var listPosts = await adminClient.GetFromJsonAsync<PagedResult<PostListItemDto>>("/api/posts");
        Assert.DoesNotContain(listPosts!.Items, p => p.Id == post.Id);

        // Act 3 - Verify anonymous cannot view trash
        var anonTrash = await anonClient.GetAsync("/api/posts/trash");
        Assert.Equal(HttpStatusCode.Unauthorized, anonTrash.StatusCode);

        // Act 4 - Admin lists Trash and finds the post
        var trashResponse = await adminClient.GetFromJsonAsync<PagedResult<TrashPostItemDto>>("/api/posts/trash");
        Assert.NotNull(trashResponse);
        var trashItem = trashResponse.Items.FirstOrDefault(p => p.Id == post.Id);
        Assert.NotNull(trashItem);
        Assert.NotNull(trashItem.DeletedAt);

        // Act 5 - Restore post via POST /api/posts/{id}/restore
        var restoreResponse = await adminClient.PostAsync($"/api/posts/{post.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        var restoredPost = await restoreResponse.Content.ReadFromJsonAsync<PostDetailDto>();
        Assert.NotNull(restoredPost);
        Assert.False(restoredPost.IsDeleted);
        Assert.Null(restoredPost.DeletedAt);
        Assert.Equal(PostStatus.Published, restoredPost.Status);

        // Post is now accessible again
        var getRestored = await adminClient.GetAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.OK, getRestored.StatusCode);

        // Act 6 - Force Delete via DELETE /api/posts/{id}/force
        var forceDeleteResponse = await adminClient.DeleteAsync($"/api/posts/{post.Id}/force");
        Assert.Equal(HttpStatusCode.NoContent, forceDeleteResponse.StatusCode);

        var trashAfterPurge = await adminClient.GetFromJsonAsync<PagedResult<TrashPostItemDto>>("/api/posts/trash");
        Assert.DoesNotContain(trashAfterPurge!.Items, p => p.Id == post.Id);
    }

    [Fact]
    public void PostHelpers_ToListItemDto_WhenAuthorIsDeletedInSupabase_HandlesGracefullyWithoutThrowing()
    {
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Orphaned Post",
            Slug = "orphaned-post",
            Summary = "Summary",
            Content = "Content",
            Author = null!,
            Status = PostStatus.Published,
            CreatedAt = DateTime.UtcNow,
        };

        var dto = PostHelpers.ToListItemDto(post, "http://localhost:5065");

        Assert.NotNull(dto);
        Assert.NotNull(dto.Author);
        Assert.Equal("Author", dto.Author.DisplayName);
    }
}
