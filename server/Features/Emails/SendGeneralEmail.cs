using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Emails;

public static class SendGeneralEmailEndpoint
{
    public static RouteGroupBuilder MapSendGeneralEmail(this RouteGroupBuilder group)
    {
        group.MapPost("/send", async (
            [FromBody] SendGeneralEmailRequest request,
            IEmailService emailService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.To) || !request.To.Contains('@'))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Recipient",
                    detail: "A valid recipient email address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Subject",
                    detail: "Email subject is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Headline) || string.IsNullOrWhiteSpace(request.BodyHtml))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Content",
                    detail: "Headline and BodyHtml content are required."
                );
            }

            var model = new GeneralEmailModel(
                RecipientEmail: request.To.Trim(),
                RecipientName: request.RecipientName?.Trim() ?? "Valued Reader",
                Subject: request.Subject.Trim(),
                Headline: request.Headline.Trim(),
                BodyHtml: request.BodyHtml.Trim(),
                ActionUrl: request.ActionUrl?.Trim(),
                ActionText: request.ActionText?.Trim()
            );

            var result = await emailService.SendGeneralNotificationAsync(model, ct);

            return Results.Ok(new EmailResponse(
                Success: result.Success,
                MessageId: result.MessageId,
                Message: result.Success
                    ? "Email dispatched successfully."
                    : result.ErrorMessage ?? "Email delivery failed or operated in offline mode."
            ));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SendGeneralEmail")
        .WithSummary("Send general email or notification broadcast (Admin Only)");

        return group;
    }
}
