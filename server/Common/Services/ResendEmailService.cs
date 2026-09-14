using deblog.Server.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace deblog.Server.Common.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend? _resendClient;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public bool IsConfigured => _resendClient != null;
    public string FromEmail => _fromEmail;
    public string FromName => _fromName;

    public ResendEmailService(
        IConfiguration config,
        ILogger<ResendEmailService> logger,
        IResend? resendClient = null)
    {
        _logger = logger;
        _fromEmail = config["RESEND_FROM_EMAIL"]?.Trim() ?? "onboarding@resend.dev";
        _fromName = config["RESEND_FROM_NAME"]?.Trim() ?? "deblog";
        _resendClient = resendClient;

        if (_resendClient != null)
        {
            _logger.LogInformation("Resend email service initialized with active client (Sender: {FromEmail}).", _fromEmail);
        }
        else
        {
            _logger.LogWarning("Resend client is not configured. Outgoing emails will operate in offline/logged mode.");
        }
    }

    public async Task<EmailSendResult> SendEmailAsync(EmailMessageRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.To))
        {
            return new EmailSendResult(false, null, "Recipient email address cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return new EmailSendResult(false, null, "Email subject cannot be empty.");
        }

        if (_resendClient == null)
        {
            _logger.LogWarning(
                "[OFFLINE-EMAIL] To: {To} | Subject: {Subject}\nText Preview: {Text}",
                request.To,
                request.Subject,
                request.TextBody
            );
            return new EmailSendResult(
                Success: false,
                MessageId: null,
                ErrorMessage: "Resend API is not configured on the server. Email logged in offline mode."
            );
        }

        try
        {
            var sender = !string.IsNullOrWhiteSpace(request.From)
                ? request.From
                : $"{_fromName} <{_fromEmail}>";

            var message = new EmailMessage
            {
                From = sender,
                To = { request.To },
                Subject = request.Subject,
                HtmlBody = request.HtmlBody,
                TextBody = request.TextBody
            };

            if (!string.IsNullOrWhiteSpace(request.ReplyTo))
            {
                message.ReplyTo ??= new();
                message.ReplyTo.Add(request.ReplyTo);
            }

            var response = await _resendClient.EmailSendAsync(message, ct);

            _logger.LogInformation(
                "Email successfully dispatched via Resend to {To} (MessageId: {MessageId}).",
                request.To,
                response.Content
            );

            return new EmailSendResult(
                Success: true,
                MessageId: response.Content.ToString(),
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend API error while sending email to {To}: {Message}", request.To, ex.Message);
            return new EmailSendResult(
                Success: false,
                MessageId: null,
                ErrorMessage: ex.Message
            );
        }
    }

    public Task<EmailSendResult> SendOnboardingOtpAsync(string recipientEmail, string recipientName, string otpCode, CancellationToken ct = default)
    {
        var (html, text) = EmailTemplateRenderer.RenderOnboardingOtp(recipientName, otpCode, recipientEmail);
        var request = new EmailMessageRequest(
            To: recipientEmail,
            Subject: $"[{_fromName}] Your account verification code: {otpCode}",
            HtmlBody: html,
            TextBody: text
        );
        return SendEmailAsync(request, ct);
    }

    public Task<EmailSendResult> SendPasswordResetOtpAsync(string recipientEmail, string recipientName, string otpCode, CancellationToken ct = default)
    {
        var (html, text) = EmailTemplateRenderer.RenderPasswordResetOtp(recipientName, otpCode, recipientEmail);
        var request = new EmailMessageRequest(
            To: recipientEmail,
            Subject: $"[{_fromName}] Password reset code: {otpCode}",
            HtmlBody: html,
            TextBody: text
        );
        return SendEmailAsync(request, ct);
    }

    public Task<EmailSendResult> SendCommentNotificationAsync(CommentNotificationModel model, CancellationToken ct = default)
    {
        var (html, text) = EmailTemplateRenderer.RenderCommentNotification(model);
        var request = new EmailMessageRequest(
            To: model.RecipientEmail,
            Subject: $"[{_fromName}] New comment on \"{model.PostTitle}\"",
            HtmlBody: html,
            TextBody: text
        );
        return SendEmailAsync(request, ct);
    }

    public Task<EmailSendResult> SendGeneralNotificationAsync(GeneralEmailModel model, CancellationToken ct = default)
    {
        var (html, text) = EmailTemplateRenderer.RenderGeneralNotification(model);
        var request = new EmailMessageRequest(
            To: model.RecipientEmail,
            Subject: model.Subject,
            HtmlBody: html,
            TextBody: text
        );
        return SendEmailAsync(request, ct);
    }
}
