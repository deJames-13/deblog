using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Emails;

public static class EmailEndpoints
{
    public static IEndpointRouteBuilder MapEmailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/emails")
            .WithTags("Emails");

        group.MapGetEmailStatus();
        group.MapSendGeneralEmail();
        group.MapSendOtpEmails();
        group.MapSendCommentNotification();

        return endpoints;
    }
}
