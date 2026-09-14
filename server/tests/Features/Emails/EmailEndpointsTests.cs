using System.Net;
using System.Net.Http.Json;
using deblog.Server.Common.Services;
using deblog.Server.Features.Emails;
using deblog.Server.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace server.Tests.Features.Emails;

public class EmailEndpointsTests : IClassFixture<EmailTestFactory>
{
    private readonly EmailTestFactory _factory;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _anonymousClient;

    public EmailEndpointsTests(EmailTestFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAdminClient();
        _anonymousClient = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task GetEmailStatus_ReturnsOkWithConfiguration()
    {
        var response = await _anonymousClient.GetAsync("/api/emails/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<EmailStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.Configured);
        Assert.Equal("online", status.Status);
        Assert.False(string.IsNullOrWhiteSpace(status.FromEmail));
    }

    [Fact]
    public async Task SendGeneralEmail_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new SendGeneralEmailRequest(
            To: "reader@example.com",
            Subject: "Test Announcement",
            Headline: "Hello",
            BodyHtml: "<p>Announcement content</p>"
        );

        var response = await _anonymousClient.PostAsJsonAsync("/api/emails/send", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendGeneralEmail_WithAdminAuth_DispatchesSuccessfully()
    {
        var request = new SendGeneralEmailRequest(
            To: "community@example.com",
            Subject: "deblog Release Notes",
            Headline: "Version 2.0 Live",
            BodyHtml: "<p>New email architecture deployed.</p>",
            RecipientName: "Community",
            ActionUrl: "https://deblog.derickespinosa.site",
            ActionText: "Read Release"
        );

        var response = await _adminClient.PostAsJsonAsync("/api/emails/send", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmailResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.MessageId));
    }

    [Fact]
    public async Task SendOnboardingOtp_PublicAccess_DispatchesSuccessfully()
    {
        var request = new SendOtpEmailRequest(
            To: "newuser@example.com",
            OtpCode: "583912",
            RecipientName: "New User"
        );

        var response = await _anonymousClient.PostAsJsonAsync("/api/emails/otp/onboarding", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmailResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendOnboardingOtp_WithInvalidEmail_ReturnsBadRequest()
    {
        var request = new SendOtpEmailRequest(
            To: "not-an-email",
            OtpCode: "123456"
        );

        var response = await _anonymousClient.PostAsJsonAsync("/api/emails/otp/onboarding", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendPasswordResetOtp_PublicAccess_DispatchesSuccessfully()
    {
        var request = new SendOtpEmailRequest(
            To: "forgot@example.com",
            OtpCode: "847291",
            RecipientName: "Account Owner"
        );

        var response = await _anonymousClient.PostAsJsonAsync("/api/emails/otp/password-reset", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmailResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendCommentNotification_WithAdminAuth_DispatchesSuccessfully()
    {
        var request = new SendCommentNotificationRequest(
            RecipientEmail: "author@deblog.local",
            PostTitle: "Building Clean Vertical Slices",
            PostSlug: "building-clean-vertical-slices",
            CommenterName: "Marcus",
            CommentContent: "Great architecture breakdown!",
            RecipientName: "Derick"
        );

        var response = await _adminClient.PostAsJsonAsync("/api/emails/comment-notification", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmailResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}

public class EmailTestFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);

            services.AddSingleton<IEmailService, MockEmailService>();
        });
    }

    public class MockEmailService : IEmailService
    {
        public bool IsConfigured => true;
        public string FromEmail => "notifications@deblog.derickespinosa.site";
        public string FromName => "deblog";

        public Task<EmailSendResult> SendEmailAsync(EmailMessageRequest request, CancellationToken ct = default)
        {
            return Task.FromResult(new EmailSendResult(true, Guid.NewGuid().ToString(), null));
        }

        public Task<EmailSendResult> SendOnboardingOtpAsync(string recipientEmail, string recipientName, string otpCode, CancellationToken ct = default)
        {
            return Task.FromResult(new EmailSendResult(true, Guid.NewGuid().ToString(), null));
        }

        public Task<EmailSendResult> SendPasswordResetOtpAsync(string recipientEmail, string recipientName, string otpCode, CancellationToken ct = default)
        {
            return Task.FromResult(new EmailSendResult(true, Guid.NewGuid().ToString(), null));
        }

        public Task<EmailSendResult> SendCommentNotificationAsync(CommentNotificationModel model, CancellationToken ct = default)
        {
            return Task.FromResult(new EmailSendResult(true, Guid.NewGuid().ToString(), null));
        }

        public Task<EmailSendResult> SendGeneralNotificationAsync(GeneralEmailModel model, CancellationToken ct = default)
        {
            return Task.FromResult(new EmailSendResult(true, Guid.NewGuid().ToString(), null));
        }
    }
}
