using deblog.Server.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Resend;

namespace server.Tests.Common;

public class ResendEmailServiceTests
{
    private static IConfiguration CreateConfig(string? apiKey = null, string? fromEmail = null, string? fromName = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["RESEND_API_KEY"] = apiKey,
            ["RESEND_FROM_EMAIL"] = fromEmail ?? "notifications@deblog.derickespinosa.site",
            ["RESEND_FROM_NAME"] = fromName ?? "deblog"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public void TemplateRenderer_OnboardingOtp_ContainsRemoteLogoAndOtp()
    {
        var (html, text) = EmailTemplateRenderer.RenderOnboardingOtp("Derick", "984210", "derick@example.com");

        Assert.Contains("https://deblog.derickespinosa.site/assets/images/logo.png", html);
        Assert.Contains("https://deblog.derickespinosa.site/assets/images/title-no-bg-light.png", html);
        Assert.Contains("984210", html);
        Assert.Contains("Derick", html);
        Assert.Contains("10 minutes", html);
        Assert.Contains("984210", text);
    }

    [Fact]
    public void TemplateRenderer_PasswordResetOtp_ContainsSecurityWarning()
    {
        var (html, text) = EmailTemplateRenderer.RenderPasswordResetOtp("Derick", "123456", "derick@example.com");

        Assert.Contains("Password Reset Request", html);
        Assert.Contains("123456", html);
        Assert.Contains("Never share this code with anyone", html);
        Assert.Contains("123456", text);
    }

    [Fact]
    public void TemplateRenderer_CommentNotification_ContainsQuoteAndPostLink()
    {
        var model = new CommentNotificationModel(
            RecipientEmail: "author@example.com",
            RecipientName: "Author",
            PostTitle: "Building Modern Angular with Signals",
            PostSlug: "building-modern-angular-with-signals",
            CommenterName: "Alice Reader",
            CommentContent: "Brilliant explanation of computed signals!"
        );

        var (html, text) = EmailTemplateRenderer.RenderCommentNotification(model);

        Assert.Contains("Building Modern Angular with Signals", html);
        Assert.Contains("Alice Reader", html);
        Assert.Contains("Brilliant explanation of computed signals!", html);
        Assert.Contains("https://deblog.derickespinosa.site/post/building-modern-angular-with-signals", html);
        Assert.Contains("Brilliant explanation of computed signals!", text);
    }

    [Fact]
    public void TemplateRenderer_EscapesMaliciousInput()
    {
        var maliciousName = "<script>alert('xss')</script>";
        var (html, _) = EmailTemplateRenderer.RenderOnboardingOtp(maliciousName, "111222", "test@example.com");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
    }

    [Fact]
    public async Task SendEmailAsync_WhenNotConfigured_LogsAndReturnsOfflineResult()
    {
        var config = CreateConfig(null);
        var service = new ResendEmailService(config, NullLogger<ResendEmailService>.Instance);

        Assert.False(service.IsConfigured);

        var request = new EmailMessageRequest("reader@example.com", "Test Subject", "<p>Hello</p>", "Hello");
        var result = await service.SendEmailAsync(request);

        Assert.False(result.Success);
        Assert.Null(result.MessageId);
        Assert.Contains("offline mode", result.ErrorMessage);
    }

    private static ResendResponse<T> CreateResponse<T>(T content)
    {
        var ctor = typeof(ResendResponse<T>).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(T), typeof(ResendRateLimit) },
            null);

        if (ctor != null)
        {
            return (ResendResponse<T>)ctor.Invoke(new object?[] { content, null })!;
        }

        var obj = (ResendResponse<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ResendResponse<T>));
        var prop = typeof(ResendResponse<T>).GetProperty("Content", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, content);
        return obj;
    }

    [Fact]
    public async Task SendEmailAsync_WithConfiguredClient_DispatchesSuccessfully()
    {
        var expectedGuid = Guid.NewGuid();
        var fakeResend = Substitute.For<IResend>();
        EmailMessage? capturedMessage = null;
        fakeResend.EmailSendAsync(Arg.Do<EmailMessage>(m => capturedMessage = m), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse(expectedGuid)));

        var config = CreateConfig("re_live_test_key_123");
        var service = new ResendEmailService(config, NullLogger<ResendEmailService>.Instance, fakeResend);

        Assert.True(service.IsConfigured);

        var request = new EmailMessageRequest(
            To: "alice@example.com",
            Subject: "Welcome to deblog",
            HtmlBody: "<p>Welcome</p>",
            TextBody: "Welcome"
        );

        var result = await service.SendEmailAsync(request);

        Assert.True(result.Success);
        Assert.Equal(expectedGuid.ToString(), result.MessageId);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(capturedMessage);
        Assert.Equal("alice@example.com", capturedMessage.To[0]);
        Assert.Equal("Welcome to deblog", capturedMessage.Subject);
    }

    [Fact]
    public async Task SendEmailAsync_WhenExceptionThrown_ReturnsFailureResult()
    {
        var fakeResend = Substitute.For<IResend>();
        fakeResend.EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task<ResendResponse<Guid>>>(_ => throw new HttpRequestException("Network failure"));

        var config = CreateConfig("re_live_test_key_123");
        var service = new ResendEmailService(config, NullLogger<ResendEmailService>.Instance, fakeResend);

        var request = new EmailMessageRequest(
            To: "alice@example.com",
            Subject: "Welcome to deblog",
            HtmlBody: "<p>Welcome</p>",
            TextBody: "Welcome"
        );

        var result = await service.SendEmailAsync(request);

        Assert.False(result.Success);
        Assert.Null(result.MessageId);
        Assert.Contains("Network failure", result.ErrorMessage);
    }

    [Fact]
    public async Task SendOnboardingOtpAsync_DispatchesWithFormattedTemplate()
    {
        var fakeResend = Substitute.For<IResend>();
        EmailMessage? captured = null;
        fakeResend.EmailSendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse(Guid.NewGuid())));

        var config = CreateConfig("re_live_test_key_123");
        var service = new ResendEmailService(config, NullLogger<ResendEmailService>.Instance, fakeResend);

        var result = await service.SendOnboardingOtpAsync("newuser@example.com", "New User", "654321");

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal("newuser@example.com", captured.To[0]);
        Assert.Contains("654321", captured.Subject);
        Assert.Contains("654321", captured.HtmlBody);
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_DispatchesWithSecurityWarning()
    {
        var fakeResend = Substitute.For<IResend>();
        EmailMessage? captured = null;
        fakeResend.EmailSendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse(Guid.NewGuid())));

        var config = CreateConfig("re_live_test_key_123");
        var service = new ResendEmailService(config, NullLogger<ResendEmailService>.Instance, fakeResend);

        var result = await service.SendPasswordResetOtpAsync("lostpwd@example.com", "Lost User", "998877");

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal("lostpwd@example.com", captured.To[0]);
        Assert.Contains("998877", captured.Subject);
        Assert.Contains("Never share this code with anyone", captured.HtmlBody);
    }

    [Fact]
    public async Task SendCommentNotificationAsync_DispatchesWithPostAndCommenter()
    {
        var fakeResend = Substitute.For<IResend>();
        EmailMessage? captured = null;
        fakeResend.EmailSendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateResponse(Guid.NewGuid())));

        var config = CreateConfig("re_live_test_key_123");
        var service = new ResendEmailService(config, NullLogger<ResendEmailService>.Instance, fakeResend);

        var model = new CommentNotificationModel(
            RecipientEmail: "author@example.com",
            RecipientName: "Author",
            PostTitle: "Awesome Post",
            PostSlug: "awesome-post",
            CommenterName: "Bob Reader",
            CommentContent: "Great article!"
        );

        var result = await service.SendCommentNotificationAsync(model);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal("author@example.com", captured.To[0]);
        Assert.Contains("Awesome Post", captured.Subject);
        Assert.Contains("Bob Reader", captured.HtmlBody);
    }
}
