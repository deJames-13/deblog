using System.Net;
using System.Text;

namespace deblog.Server.Common.Services;

public static class EmailTemplateRenderer
{
    private const string LogoUrl = "https://deblog.derickespinosa.site/assets/images/logo.png";
    private const string TitleLogoUrl = "https://deblog.derickespinosa.site/assets/images/title-no-bg-light.png";
    private const string SiteUrl = "https://deblog.derickespinosa.site";
    private const string SiteName = "deblog";

    public static (string Html, string Text) RenderOnboardingOtp(string name, string otpCode, string recipientEmail)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeCode = WebUtility.HtmlEncode(otpCode);
        var currentYear = DateTime.UtcNow.Year.ToString();

        var bodyHtml = $"""
            <h2 style="margin: 0 0 12px 0; font-size: 20px; font-weight: 700; color: #0f172a; letter-spacing: -0.01em;">
                Welcome to {SiteName}, {safeName}
            </h2>
            <p style="margin: 0 0 16px 0; font-size: 14px; line-height: 1.6; color: #475569;">
                Thank you for joining our community. Please enter the one-time verification code below to verify your email address and activate your account:
            </p>
            <div style="margin: 24px 0; text-align: center;">
                <div style="display: inline-block; padding: 14px 28px; font-size: 30px; font-weight: 700; letter-spacing: 8px; color: #0f172a; background-color: #f8fafc; border: 1px solid #cbd5e1; border-radius: 6px; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace;">
                    {safeCode}
                </div>
            </div>
            <p style="margin: 0 0 8px 0; font-size: 13px; line-height: 1.5; color: #64748b;">
                <strong>Security Notice:</strong> This code is valid for 10 minutes. If you did not create an account on {SiteName}, please disregard this email.
            </p>
        """;

        var html = WrapBaseTemplate("Verify your account", $"Your verification code is {safeCode}", bodyHtml, recipientEmail, currentYear);

        var text = $"""
        Welcome to {SiteName}, {name}!

        Please use the following one-time code to verify your email address:
        {otpCode}

        This code is valid for 10 minutes. If you did not initiate this request, you can safely ignore this email.

        {SiteName} — {SiteUrl}
        """;

        return (html, text.Trim());
    }

    public static (string Html, string Text) RenderPasswordResetOtp(string name, string otpCode, string recipientEmail)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeCode = WebUtility.HtmlEncode(otpCode);
        var currentYear = DateTime.UtcNow.Year.ToString();

        var bodyHtml = $"""
            <h2 style="margin: 0 0 12px 0; font-size: 20px; font-weight: 700; color: #0f172a; letter-spacing: -0.01em;">
                Password Reset Request
            </h2>
            <p style="margin: 0 0 16px 0; font-size: 14px; line-height: 1.6; color: #475569;">
                Hello {safeName}, we received a request to reset your password. Use the security code below to proceed with updating your credentials:
            </p>
            <div style="margin: 24px 0; text-align: center;">
                <div style="display: inline-block; padding: 14px 28px; font-size: 30px; font-weight: 700; letter-spacing: 8px; color: #0f172a; background-color: #f8fafc; border: 1px solid #cbd5e1; border-radius: 6px; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace;">
                    {safeCode}
                </div>
            </div>
            <p style="margin: 0 0 8px 0; font-size: 13px; line-height: 1.5; color: #dc2626;">
                <strong>Never share this code with anyone.</strong> {SiteName} administrators will never ask for your security code.
            </p>
            <p style="margin: 0 0 8px 0; font-size: 13px; line-height: 1.5; color: #64748b;">
                This code expires in 10 minutes. If you did not request a password reset, you can safely ignore this message—your account remains secure.
            </p>
        """;

        var html = WrapBaseTemplate("Reset your password", $"Your password reset code is {safeCode}", bodyHtml, recipientEmail, currentYear);

        var text = $"""
        Password Reset Request for {name}

        Use the following one-time code to reset your password:
        {otpCode}

        Never share this code with anyone. It expires in 10 minutes.
        If you did not request this, please ignore this email.

        {SiteName} — {SiteUrl}
        """;

        return (html, text.Trim());
    }

    public static (string Html, string Text) RenderCommentNotification(CommentNotificationModel model)
    {
        var safeRecipient = WebUtility.HtmlEncode(model.RecipientName);
        var safeAuthor = WebUtility.HtmlEncode(model.CommenterName);
        var safeTitle = WebUtility.HtmlEncode(model.PostTitle);
        var safeContent = WebUtility.HtmlEncode(model.CommentContent);
        var postUrl = $"{SiteUrl}/post/{model.PostSlug}";
        var currentYear = DateTime.UtcNow.Year.ToString();

        var bodyHtml = $"""
            <h2 style="margin: 0 0 12px 0; font-size: 20px; font-weight: 700; color: #0f172a; letter-spacing: -0.01em;">
                New Discussion on "{safeTitle}"
            </h2>
            <p style="margin: 0 0 16px 0; font-size: 14px; line-height: 1.6; color: #475569;">
                Hello {safeRecipient}, <strong>{safeAuthor}</strong> just posted a new comment on your article:
            </p>
            <div style="margin: 16px 0; padding: 14px 18px; background-color: #f8fafc; border-left: 3px solid #0f172a; border-radius: 0 6px 6px 0; color: #334155; font-size: 14px; line-height: 1.6; font-style: italic;">
                "{safeContent}"
            </div>
            <div style="margin: 24px 0 16px 0;">
                <a href="{postUrl}" style="display: inline-block; padding: 11px 22px; font-size: 13px; font-weight: 600; color: #ffffff; background-color: #0f172a; border-radius: 6px; text-decoration: none; letter-spacing: 0.02em;">
                    View Discussion & Reply →
                </a>
            </div>
        """;

        var html = WrapBaseTemplate($"New comment on {safeTitle}", $"{safeAuthor} commented: {safeContent}", bodyHtml, model.RecipientEmail, currentYear);

        var text = $"""
        New comment on "{model.PostTitle}" from {model.CommenterName}:

        "{model.CommentContent}"

        Read and reply: {postUrl}

        {SiteName} — {SiteUrl}
        """;

        return (html, text.Trim());
    }

    public static (string Html, string Text) RenderGeneralNotification(GeneralEmailModel model)
    {
        var safeRecipient = WebUtility.HtmlEncode(model.RecipientName);
        var safeHeadline = WebUtility.HtmlEncode(model.Headline);
        var currentYear = DateTime.UtcNow.Year.ToString();

        var ctaHtml = string.IsNullOrWhiteSpace(model.ActionUrl)
            ? string.Empty
            : $"""
            <div style="margin: 24px 0 16px 0;">
                <a href="{WebUtility.HtmlEncode(model.ActionUrl)}" style="display: inline-block; padding: 11px 22px; font-size: 13px; font-weight: 600; color: #ffffff; background-color: #0f172a; border-radius: 6px; text-decoration: none; letter-spacing: 0.02em;">
                    {WebUtility.HtmlEncode(model.ActionText ?? "Read More →")}
                </a>
            </div>
            """;

        var bodyHtml = $"""
            <h2 style="margin: 0 0 12px 0; font-size: 20px; font-weight: 700; color: #0f172a; letter-spacing: -0.01em;">
                {safeHeadline}
            </h2>
            <p style="margin: 0 0 16px 0; font-size: 14px; line-height: 1.6; color: #475569;">
                Hello {safeRecipient},
            </p>
            <div style="font-size: 14px; line-height: 1.6; color: #334155;">
                {model.BodyHtml}
            </div>
            {ctaHtml}
        """;

        var html = WrapBaseTemplate(model.Subject, model.Headline, bodyHtml, model.RecipientEmail, currentYear);

        var text = $"""
        {model.Subject}

        Hello {model.RecipientName},

        {model.Headline}

        {StripHtml(model.BodyHtml)}

        {(string.IsNullOrWhiteSpace(model.ActionUrl) ? "" : $"{model.ActionText ?? "Link"}: {model.ActionUrl}")}

        {SiteName} — {SiteUrl}
        """;

        return (html, text.Trim());
    }

    private static string WrapBaseTemplate(string title, string preheader, string bodyContent, string recipientEmail, string currentYear)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safePreheader = WebUtility.HtmlEncode(preheader);
        var safeRecipient = WebUtility.HtmlEncode(recipientEmail);

        return $"""
        <!DOCTYPE html>
        <html lang="en" xmlns="http://www.w3.org/1999/xhtml">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>{safeTitle}</title>
            <!--[if mso]>
            <xml>
                <o:OfficeDocumentSettings>
                    <o:PixelsPerInch>96</o:PixelsPerInch>
                </o:OfficeDocumentSettings>
            </xml>
            <![endif]-->
        </head>
        <body style="margin:0;padding:0;background-color:#f8fafc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#0f172a;-webkit-font-smoothing:antialiased;">
            <!-- Preheader text for preview in mail clients -->
            <div style="display:none;font-size:1px;color:#f8fafc;line-height:1px;max-height:0px;max-width:0px;opacity:0;overflow:hidden;">
                {safePreheader}
            </div>

            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f8fafc;margin:0;padding:32px 16px;">
                <tr>
                    <td align="center">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:580px;background-color:#ffffff;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;">
                            <!-- Header / Logo -->
                            <tr>
                                <td style="padding:28px 32px 20px 32px;border-bottom:1px solid #f1f5f9;">
                                    <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                                        <tr>
                                            <td style="vertical-align:middle;padding-right:12px;">
                                                <a href="{SiteUrl}" target="_blank" style="text-decoration:none;">
                                                    <img src="{LogoUrl}" alt="{SiteName}" width="38" height="38" style="display:block;border:0;border-radius:6px;width:38px;height:38px;" />
                                                </a>
                                            </td>
                                            <td style="vertical-align:middle;">
                                                <a href="{SiteUrl}" target="_blank" style="text-decoration:none;display:inline-block;">
                                                    <img src="{TitleLogoUrl}" alt="{SiteName}" height="22" style="display:block;border:0;height:22px;" />
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>

                            <!-- Main Body Content -->
                            <tr>
                                <td style="padding:32px 32px 28px 32px;">
                                    {bodyContent}
                                </td>
                            </tr>

                            <!-- Footer -->
                            <tr>
                                <td style="padding:20px 32px;background-color:#fafafa;border-top:1px solid #f1f5f9;font-size:12px;line-height:1.6;color:#94a3b8;">
                                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                                        <tr>
                                            <td>
                                                <p style="margin:0 0 4px 0;font-weight:600;color:#64748b;">
                                                    <a href="{SiteUrl}" target="_blank" style="color:#0f172a;text-decoration:none;">{SiteName}</a> — Minimalist Engineering & Systems Architecture
                                                </p>
                                                <p style="margin:0 0 4px 0;">
                                                    This email was delivered to <span style="color:#475569;">{safeRecipient}</span>.
                                                </p>
                                                <p style="margin:0;font-size:11px;color:#cbd5e1;">
                                                    &copy; {currentYear} Derick Espinosa. All rights reserved.
                                                </p>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var array = new char[input.Length];
        var arrayIndex = 0;
        var inside = false;

        for (var i = 0; i < input.Length; i++)
        {
            var let = input[i];
            if (let == '<')
            {
                inside = true;
                continue;
            }
            if (let == '>')
            {
                inside = false;
                continue;
            }
            if (!inside)
            {
                array[arrayIndex++] = let;
            }
        }
        return new string(array, 0, arrayIndex).Trim();
    }
}
