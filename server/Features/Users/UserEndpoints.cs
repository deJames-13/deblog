namespace deblog.Server.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var userGroup = app.MapGroup("/api/users")
            .WithTags("Users");

        userGroup.MapGetCurrentUser();
        userGroup.MapUpdateCurrentUser();
        userGroup.MapGetUserById();

        var adminGroup = app.MapGroup("/api/admin/users")
            .WithTags("Admin Users")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapAdminListUsers();
        adminGroup.MapAdminGetTrashUsers();
        adminGroup.MapAdminCreateUser();
        adminGroup.MapAdminUpdateUser();
        adminGroup.MapAdminUpdateUserStatus();
        adminGroup.MapAdminSoftDeleteUser();
        adminGroup.MapAdminRestoreUser();
        adminGroup.MapAdminForceDeleteUser();

        return app;
    }
}
