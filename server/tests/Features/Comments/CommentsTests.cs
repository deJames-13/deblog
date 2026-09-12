using System.Net;
using System.Net.Http.Json;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Posts;
using deblog.Server.Tests.Common;

namespace deblog.Server.Tests.Features.Comments;

public class CommentsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CommentsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<PostDetailDto> CreateTestPostAsync()
    {
        var adminClient = _factory.CreateAdminClient();
        var response = await adminClient.PostAsJsonAsync("/api/posts",
            new CreatePostRequest($"Post for Comments {Guid.NewGuid().ToString()[..6]}", null, "Summary", "Content", true));
        return (await response.Content.ReadFromJsonAsync<PostDetailDto>())!;
    }

    [Fact]
    public async Task Guest_CanPostComment_WithValidEmail_ReturnsPendingAndToken()
    {
        // Arrange
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();

        var commentReq = new CreateGuestCommentRequest(
            "Great article! Really enjoyed reading it.",
            "guest.reader@example.com",
            "Jane Doe"
        );

        // Act
        var response = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments", commentReq);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = await response.Content.ReadFromJsonAsync<CommentCreatedResponseDto>();

        Assert.NotNull(comment);
        Assert.Equal(CommentStatus.Pending, comment.Status);
        Assert.NotEqual(Guid.Empty, comment.ManagementToken);
        Assert.Equal("Jane Doe", comment.Author.DisplayName);
    }

    [Fact]
    public async Task Guest_CannotPostComment_WithInvalidEmail()
    {
        // Arrange
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();
        var invalidReq = new CreateGuestCommentRequest("Hello", "not-a-valid-email", "Anonymous");

        // Act
        var response = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments", invalidReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicFeed_ShowsOnlyApprovedComments()
    {
        // Arrange
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();

        // 1. Post a guest comment (status = Pending)
        var createResp = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new CreateGuestCommentRequest("Pending comment", "guest1@example.com", "Guest 1"));
        var comment = await createResp.Content.ReadFromJsonAsync<CommentCreatedResponseDto>();
        Assert.NotNull(comment);

        // 2. Public feed should NOT list pending comments
        var publicFeedResp = await anonClient.GetAsync($"/api/posts/{post.Id}/comments");
        Assert.Equal(HttpStatusCode.OK, publicFeedResp.StatusCode);
        var publicComments = await publicFeedResp.Content.ReadFromJsonAsync<List<CommentResponseDto>>();
        Assert.NotNull(publicComments);
        Assert.DoesNotContain(publicComments, c => c.Id == comment.Id);

        // 3. Admin approves the comment
        var adminClient = _factory.CreateAdminClient();
        var approveResp = await adminClient.PatchAsJsonAsync($"/api/admin/comments/{comment.Id}/status",
            new UpdateCommentStatusRequest(CommentStatus.Approved));
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        // 4. Now public feed MUST contain the approved comment
        var updatedFeedResp = await anonClient.GetAsync($"/api/posts/{post.Id}/comments");
        var updatedComments = await updatedFeedResp.Content.ReadFromJsonAsync<List<CommentResponseDto>>();
        Assert.NotNull(updatedComments);
        Assert.Contains(updatedComments, c => c.Id == comment.Id);
    }

    [Fact]
    public async Task Guest_CanUpdateAndSoftRemoveComment_WithValidManagementToken()
    {
        // Arrange - Create comment
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();
        var createResp = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new CreateGuestCommentRequest("Original comment text", "guest2@example.com", "Guest 2"));
        var created = await createResp.Content.ReadFromJsonAsync<CommentCreatedResponseDto>();
        Assert.NotNull(created);

        // Act 1 - Update with valid X-Comment-Token header
        var updateMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/comments/{created.Id}")
        {
            Content = JsonContent.Create(new UpdateCommentRequest("Edited comment text", null))
        };
        updateMessage.Headers.Add("X-Comment-Token", created.ManagementToken.ToString());
        var updateResp = await anonClient.SendAsync(updateMessage);

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<CommentResponseDto>();
        Assert.Equal("Edited comment text", updated!.Content);

        // Act 2 - Update with INVALID token should fail
        var badUpdateMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/comments/{created.Id}")
        {
            Content = JsonContent.Create(new UpdateCommentRequest("Hacked comment", null))
        };
        badUpdateMessage.Headers.Add("X-Comment-Token", Guid.NewGuid().ToString());
        var badUpdateResp = await anonClient.SendAsync(badUpdateMessage);
        Assert.Equal(HttpStatusCode.Unauthorized, badUpdateResp.StatusCode);

        // Act 3 - Soft-remove with valid token
        var deleteMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/comments/{created.Id}");
        deleteMessage.Headers.Add("X-Comment-Token", created.ManagementToken.ToString());
        var deleteResp = await anonClient.SendAsync(deleteMessage);
        Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_CannotAccessAdminCommentModeration()
    {
        // Arrange
        var userClient = _factory.CreateUserClient("reader@example.com", "User");
        var anonClient = _factory.CreateAnonymousClient();

        // Act & Assert
        var anonResp = await anonClient.GetAsync("/api/admin/comments");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResp.StatusCode);

        var userResp = await userClient.GetAsync("/api/admin/comments");
        Assert.Equal(HttpStatusCode.Forbidden, userResp.StatusCode);
    }
}
