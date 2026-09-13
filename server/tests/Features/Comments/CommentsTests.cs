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

    [Fact]
    public async Task Guest_CanSoftDeleteComment_WithToken_AndAdminCanListInTrash()
    {
        // Arrange
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();
        var adminClient = _factory.CreateAdminClient();

        // 1. Guest creates comment
        var createResp = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new CreateGuestCommentRequest("Trashable comment", "trashguest@example.com", "Trash Guest"));
        var comment = await createResp.Content.ReadFromJsonAsync<CommentCreatedResponseDto>();
        Assert.NotNull(comment);

        // 2. Admin approves comment
        await adminClient.PatchAsJsonAsync($"/api/admin/comments/{comment.Id}/status",
            new UpdateCommentStatusRequest(CommentStatus.Approved));

        // 3. Guest soft deletes with token
        var deleteMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/comments/{comment.Id}");
        deleteMessage.Headers.Add("X-Comment-Token", comment.ManagementToken.ToString());
        var deleteResp = await anonClient.SendAsync(deleteMessage);
        Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);

        // 4. Public feed excludes soft-deleted comment
        var feed = await anonClient.GetFromJsonAsync<List<CommentResponseDto>>($"/api/posts/{post.Id}/comments");
        Assert.NotNull(feed);
        Assert.DoesNotContain(feed, c => c.Id == comment.Id);

        // 5. Admin trash lists soft-deleted comment
        var trashResp = await adminClient.GetFromJsonAsync<PagedResult<TrashCommentItemDto>>("/api/admin/comments/trash");
        Assert.NotNull(trashResp);
        var trashItem = trashResp.Items.FirstOrDefault(c => c.Id == comment.Id);
        Assert.NotNull(trashItem);
        Assert.NotNull(trashItem.DeletedAt);

        // 6. Non-admin cannot view comment trash
        var anonTrash = await anonClient.GetAsync("/api/admin/comments/trash");
        Assert.Equal(HttpStatusCode.Unauthorized, anonTrash.StatusCode);
    }

    [Fact]
    public async Task Admin_CanRestoreAndForceDeleteComment()
    {
        // Arrange
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();
        var adminClient = _factory.CreateAdminClient();

        var createResp = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new CreateGuestCommentRequest("To restore and purge", "purgeguest@example.com", "Purge Guest"));
        var comment = await createResp.Content.ReadFromJsonAsync<CommentCreatedResponseDto>();
        Assert.NotNull(comment);

        // Soft delete
        var deleteMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/comments/{comment.Id}");
        deleteMessage.Headers.Add("X-Comment-Token", comment.ManagementToken.ToString());
        await anonClient.SendAsync(deleteMessage);

        // Act 1 - Admin restores comment
        var restoreResp = await adminClient.PostAsync($"/api/admin/comments/{comment.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResp.StatusCode);

        // Verify no longer in trash
        var trashResp = await adminClient.GetFromJsonAsync<PagedResult<TrashCommentItemDto>>("/api/admin/comments/trash");
        Assert.NotNull(trashResp);
        Assert.DoesNotContain(trashResp.Items, c => c.Id == comment.Id);

        // Act 2 - Admin force deletes comment
        var forceResp = await adminClient.DeleteAsync($"/api/admin/comments/{comment.Id}/force");
        Assert.Equal(HttpStatusCode.NoContent, forceResp.StatusCode);

        // Verify cannot be found anywhere
        var restoreAgain = await adminClient.PostAsync($"/api/admin/comments/{comment.Id}/restore", null);
        Assert.Equal(HttpStatusCode.NotFound, restoreAgain.StatusCode);
    }

    [Fact]
    public async Task Admin_CanModerateCommentStatus_RejectedAndSpam()
    {
        // Arrange
        var post = await CreateTestPostAsync();
        var anonClient = _factory.CreateAnonymousClient();
        var adminClient = _factory.CreateAdminClient();

        var createResp = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new CreateGuestCommentRequest("Suspicious comment", "spammer@example.com", "Spammer"));
        var comment = await createResp.Content.ReadFromJsonAsync<CommentCreatedResponseDto>();
        Assert.NotNull(comment);

        // Act 1 - Mark as Spam
        var spamResp = await adminClient.PatchAsJsonAsync($"/api/admin/comments/{comment.Id}/status",
            new UpdateCommentStatusRequest(CommentStatus.Spam));
        Assert.Equal(HttpStatusCode.OK, spamResp.StatusCode);

        var feedAfterSpam = await anonClient.GetFromJsonAsync<List<CommentResponseDto>>($"/api/posts/{post.Id}/comments");
        Assert.NotNull(feedAfterSpam);
        Assert.DoesNotContain(feedAfterSpam, c => c.Id == comment.Id);

        // Act 2 - Mark as Rejected
        var rejectResp = await adminClient.PatchAsJsonAsync($"/api/admin/comments/{comment.Id}/status",
            new UpdateCommentStatusRequest(CommentStatus.Rejected));
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);

        var feedAfterReject = await anonClient.GetFromJsonAsync<List<CommentResponseDto>>($"/api/posts/{post.Id}/comments");
        Assert.NotNull(feedAfterReject);
        Assert.DoesNotContain(feedAfterReject, c => c.Id == comment.Id);
    }

    [Fact]
    public async Task SuspendedOrBannedUser_CannotPostGuestComment()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var anonClient = _factory.CreateAnonymousClient();
        var post = await CreateTestPostAsync();

        var bannedEmail = $"banned_commenter_{Guid.NewGuid().ToString()[..6]}@example.com";
        var userCreate = await adminClient.PostAsJsonAsync("/api/admin/users",
            new deblog.Server.Features.Users.AdminCreateUserRequest(bannedEmail, $"banned_{Guid.NewGuid().ToString()[..6]}", "Banned Guy", null, null, deblog.Server.Features.Users.UserRoles.User, deblog.Server.Features.Users.UserStatus.Banned));
        Assert.Equal(HttpStatusCode.Created, userCreate.StatusCode);

        // Act - Guest attempts to comment with banned email
        var commentResp = await anonClient.PostAsJsonAsync($"/api/posts/{post.Id}/comments",
            new CreateGuestCommentRequest("Trying to troll", bannedEmail, "Troll"));

        // Assert - 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, commentResp.StatusCode);
    }
}
