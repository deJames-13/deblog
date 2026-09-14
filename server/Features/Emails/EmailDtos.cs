namespace deblog.Server.Features.Emails;

public record SendGeneralEmailRequest(
    string To,
    string Subject,
    string Headline,
    string BodyHtml,
    string? RecipientName = null,
    string? ActionUrl = null,
    string? ActionText = null
);

public record SendOtpEmailRequest(
    string To,
    string OtpCode,
    string? RecipientName = null
);

public record SendCommentNotificationRequest(
    string RecipientEmail,
    string PostTitle,
    string PostSlug,
    string CommenterName,
    string CommentContent,
    string? RecipientName = null
);

public record EmailResponse(
    bool Success,
    string? MessageId,
    string Message
);

public record EmailStatusDto(
    bool Configured,
    string FromEmail,
    string FromName,
    string Status
);
