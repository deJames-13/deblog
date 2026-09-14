namespace deblog.Server.Common.Services;

public record EmailSendResult(
    bool Success,
    string? MessageId = null,
    string? ErrorMessage = null
);

public record EmailMessageRequest(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? From = null,
    string? ReplyTo = null
);

public record CommentNotificationModel(
    string RecipientEmail,
    string RecipientName,
    string PostTitle,
    string PostSlug,
    string CommenterName,
    string CommentContent
);

public record GeneralEmailModel(
    string RecipientEmail,
    string RecipientName,
    string Subject,
    string Headline,
    string BodyHtml,
    string? ActionUrl = null,
    string? ActionText = null
);

public interface IEmailService
{
    bool IsConfigured { get; }
    string FromEmail { get; }
    string FromName { get; }

    Task<EmailSendResult> SendEmailAsync(EmailMessageRequest request, CancellationToken ct = default);
    Task<EmailSendResult> SendOnboardingOtpAsync(string recipientEmail, string recipientName, string otpCode, CancellationToken ct = default);
    Task<EmailSendResult> SendPasswordResetOtpAsync(string recipientEmail, string recipientName, string otpCode, CancellationToken ct = default);
    Task<EmailSendResult> SendCommentNotificationAsync(CommentNotificationModel model, CancellationToken ct = default);
    Task<EmailSendResult> SendGeneralNotificationAsync(GeneralEmailModel model, CancellationToken ct = default);
}
