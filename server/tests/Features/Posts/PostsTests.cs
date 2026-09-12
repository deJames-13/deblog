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
}
