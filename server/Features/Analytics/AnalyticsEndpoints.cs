using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static RouteGroupBuilder MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/analytics")
            .WithTags("Analytics");

        group.MapGetDailyTelemetry();

        return group;
    }
}
