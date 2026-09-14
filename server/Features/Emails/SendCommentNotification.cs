using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace deblog.Server.Features.Emails;

public static class SendCommentNotificationEndpoint
{
    public static RouteGroupBuilder MapSendCommentNotification(this RouteGroupBuilder group)
    {
        group.MapPost("/comment-notification", async (
            [FromBody] SendCommentNotificationRequest request,
            IEmailService emailService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.RecipientEmail) || !request.RecipientEmail.Contains('@'))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Recipient",
                    detail: "A valid recipient email address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.PostTitle) || string.IsNullOrWhiteSpace(request.CommentContent))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Data",
                    detail: "PostTitle and CommentContent are required."
                );
            }

            var model = new CommentNotificationModel(
                RecipientEmail: request.RecipientEmail.Trim(),
                RecipientName: request.RecipientName?.Trim() ?? "Author",
                PostTitle: request.PostTitle.Trim(),
                PostSlug: request.PostSlug?.Trim() ?? "article",
                CommenterName: request.CommenterName?.Trim() ?? "A Reader",
                CommentContent: request.CommentContent.Trim()
            );

            var result = await emailService.SendCommentNotificationAsync(model, ct);

            return Results.Ok(new EmailResponse(
                Success: result.Success,
                MessageId: result.MessageId,
                Message: result.Success
                    ? "Comment notification email dispatched successfully."
                    : result.ErrorMessage ?? "Email dispatch failed or operated in offline mode."
            ));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("SendCommentNotification")
        .WithSummary("Dispatch notification for new comment or thread reply");

        return group;
    }
}
