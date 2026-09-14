using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/settings")
            .WithTags("Settings");

        group.MapGetSettings();
        group.MapUpdateSettings();
        group.MapUploadAvatar();
        group.MapUploadBanner();

        return endpoints;
    }
}
