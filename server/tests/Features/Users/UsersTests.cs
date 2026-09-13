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

    [Fact]
    public async Task Admin_CanUpdateUserStatus_SuspendAndBan()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var email = $"status_test_{Guid.NewGuid().ToString()[..6]}@example.com";
        var createResp = await adminClient.PostAsJsonAsync("/api/admin/users",
            new AdminCreateUserRequest(email, $"user_{Guid.NewGuid().ToString()[..6]}", "Test User", null, null, UserRoles.User));
        var user = await createResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(user);
        Assert.Equal(UserStatus.Active, user.Status);

        // Act 1 - Suspend user
        var suspendResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{user.Id}/status",
            new UpdateUserStatusRequest(UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.OK, suspendResp.StatusCode);
        var suspendedUser = await suspendResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(UserStatus.Suspended, suspendedUser!.Status);

        // Act 2 - Ban user
        var banResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{user.Id}/status",
            new UpdateUserStatusRequest(UserStatus.Banned));
        Assert.Equal(HttpStatusCode.OK, banResp.StatusCode);
        var bannedUser = await banResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(UserStatus.Banned, bannedUser!.Status);

        // Act 3 - Reinstate to Active
        var activeResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{user.Id}/status",
            new UpdateUserStatusRequest(UserStatus.Active));
        Assert.Equal(HttpStatusCode.OK, activeResp.StatusCode);
        var activeUser = await activeResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(UserStatus.Active, activeUser!.Status);
    }

    [Fact]
    public async Task SuspendedOrBannedUser_CannotAccess_Me_ReturnsForbidden()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var email = $"suspended_{Guid.NewGuid().ToString()[..6]}@example.com";
        var userClient = _factory.CreateUserClient(email, UserRoles.User);

        // Provision user profile
        var meInit = await userClient.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, meInit.StatusCode);
        var profile = await meInit.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);

        // Admin suspends user
        var suspendResp = await adminClient.PatchAsJsonAsync($"/api/admin/users/{profile.Id}/status",
            new UpdateUserStatusRequest(UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.OK, suspendResp.StatusCode);

        // Act & Assert 1 - Suspended user GET /api/users/me -> 403 Forbidden
        var meResp = await userClient.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Forbidden, meResp.StatusCode);

        // Act & Assert 2 - Suspended user PUT /api/users/me -> 403 Forbidden
        var putResp = await userClient.PutAsJsonAsync("/api/users/me", new UpdateUserProfileRequest("New Name", null, null));
        Assert.Equal(HttpStatusCode.Forbidden, putResp.StatusCode);
    }

    [Fact]
    public async Task Admin_CanSoftDelete_ListInTrash_Restore_AndForceDeleteUser()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient();
        var email = $"trash_user_{Guid.NewGuid().ToString()[..6]}@example.com";
        var createResp = await adminClient.PostAsJsonAsync("/api/admin/users",
            new AdminCreateUserRequest(email, $"trash_{Guid.NewGuid().ToString()[..6]}", "To Delete", null, null, UserRoles.User));
        var user = await createResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(user);

        // Act 1 - Soft Delete
        var softDeleteResp = await adminClient.DeleteAsync($"/api/admin/users/{user.Id}");
        Assert.Equal(HttpStatusCode.NoContent, softDeleteResp.StatusCode);

        // Act 2 - Excluded from normal queries
        var getResp = await adminClient.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        var listResp = await adminClient.GetFromJsonAsync<PagedResult<UserProfileDto>>("/api/admin/users");
        Assert.DoesNotContain(listResp!.Items, u => u.Id == user.Id);

        // Act 3 - Included in Trash
        var trashResp = await adminClient.GetFromJsonAsync<PagedResult<TrashUserItemDto>>("/api/admin/users/trash");
        Assert.NotNull(trashResp);
        var trashItem = trashResp.Items.FirstOrDefault(u => u.Id == user.Id);
        Assert.NotNull(trashItem);
        Assert.NotNull(trashItem.DeletedAt);

        // Act 4 - Non-admin cannot view trash
        var regularClient = _factory.CreateUserClient("other@example.com", UserRoles.User);
        var regTrashResp = await regularClient.GetAsync("/api/admin/users/trash");
        Assert.Equal(HttpStatusCode.Forbidden, regTrashResp.StatusCode);

        // Act 5 - Restore user
        var restoreResp = await adminClient.PostAsync($"/api/admin/users/{user.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResp.StatusCode);
        var restoredUser = await restoreResp.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(restoredUser);
        Assert.False(restoredUser.IsDeleted);
        Assert.Null(restoredUser.DeletedAt);

        // Verify active again
        var getRestored = await adminClient.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.OK, getRestored.StatusCode);

        // Act 6 - Force Delete
        var forceResp = await adminClient.DeleteAsync($"/api/admin/users/{user.Id}/force");
        Assert.Equal(HttpStatusCode.NoContent, forceResp.StatusCode);

        // Verify completely gone
        var getPurged = await adminClient.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getPurged.StatusCode);
        var trashAfterPurge = await adminClient.GetFromJsonAsync<PagedResult<TrashUserItemDto>>("/api/admin/users/trash");
        Assert.DoesNotContain(trashAfterPurge!.Items, u => u.Id == user.Id);
    }
}
