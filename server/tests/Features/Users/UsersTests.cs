using System.Net;
using System.Net.Http.Json;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Users;
using deblog.Server.Tests.Common;

namespace deblog.Server.Tests.Features.Users;

public class UsersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UsersTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousClient_AccessingMe_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_CanRetrieveAndSyncProfile()
    {
        // Arrange
        var userEmail = $"user_{Guid.NewGuid().ToString()[..6]}@example.com";
        var client = _factory.CreateUserClient(userEmail, UserRoles.User);

        // Act - First call should provision profile
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        Assert.NotNull(profile);
        Assert.Equal(userEmail, profile.Email);
        Assert.Equal(UserRoles.User, profile.Role);
    }

    [Fact]
    public async Task AuthenticatedUser_CanUpdateProfile()
    {
        // Arrange
        var userEmail = $"editor_{Guid.NewGuid().ToString()[..6]}@example.com";
        var client = _factory.CreateUserClient(userEmail, UserRoles.User);

        // Provision
        await client.GetAsync("/api/users/me");

        // Act - Update
        var updateReq = new UpdateUserProfileRequest("New Display Name", "Bio text here", "https://example.com/avatar.png");
        var updateResp = await client.PutAsJsonAsync("/api/users/me", updateReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(updated);
        Assert.Equal("New Display Name", updated.DisplayName);
        Assert.Equal("Bio text here", updated.Bio);
    }

    [Fact]
    public async Task NonAdmin_AccessingAdminUsers_ReturnsForbidden()
    {
        // Arrange
        var regularUserClient = _factory.CreateUserClient("regular@example.com", UserRoles.User);

        // Act
        var response = await regularUserClient.GetAsync("/api/admin/users");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanPerformFullUserCrud()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();

        // 1. Create a user
        var newEmail = $"created_{Guid.NewGuid().ToString()[..6]}@example.com";
        var username = $"user_{Guid.NewGuid().ToString()[..6]}";
        var createReq = new AdminCreateUserRequest(newEmail, username, "Created User", "A user bio", null, UserRoles.User);

        var createResp = await adminClient.PostAsJsonAsync("/api/admin/users", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var createdUser = await createResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(createdUser);

        // 2. List users (should include created user)
        var listResp = await adminClient.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var pagedUsers = await listResp.Content.ReadFromJsonAsync<PagedResult<UserProfileDto>>();
        Assert.NotNull(pagedUsers);
        Assert.Contains(pagedUsers.Items, u => u.Id == createdUser.Id);

        // 3. Update user
        var updateReq = new AdminUpdateUserRequest(null, null, "Updated Display Name", null, null, null);
        var updateResp = await adminClient.PutAsJsonAsync($"/api/admin/users/{createdUser.Id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updatedUser = await updateResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("Updated Display Name", updatedUser!.DisplayName);

        // 4. Delete user
        var deleteResp = await adminClient.DeleteAsync($"/api/admin/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // 5. Verify deleted from public lookup
        var getResp = await adminClient.GetAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }
}
