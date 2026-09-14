using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Emails;

public static class GetEmailStatusEndpoint
{
    public static RouteGroupBuilder MapGetEmailStatus(this RouteGroupBuilder group)
    {
        group.MapGet("/status", (IEmailService emailService) =>
        {
            var status = new EmailStatusDto(
                Configured: emailService.IsConfigured,
                FromEmail: emailService.FromEmail,
                FromName: emailService.FromName,
                Status: emailService.IsConfigured ? "online" : "offline"
            );

            return Results.Ok(status);
        })
        .WithName("GetEmailStatus")
        .WithSummary("Check status of the Resend email service");

        return group;
    }
}
