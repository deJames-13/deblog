using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using deblog.Server.Common.Data;
using deblog.Server.Common.Services;
using deblog.Server.Features.Auth;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Emails;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Users;
using deblog.Server.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Tests.Features.Emails;

namespace server.Tests.Security;

public class RateLimitingTests : IClassFixture<RateLimitingTestFactory>
{
    private readonly RateLimitingTestFactory _factory;

    public RateLimitingTests(RateLimitingTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientForIp(string ip)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
        return client;
    }

    [Fact]
    public async Task AdminLogin_Enforces5RequestsLimit_Rejects6thWith429AndRetryAfter()
    {
        var ip = $"198.51.100.{Guid.NewGuid().ToString().Substring(0, 4)}";
        var client = CreateClientForIp(ip);

        var loginPayload = new LoginRequest("admin@example.com", "WrongPassword123!");

        // First 5 attempts should be evaluated normally (401 Unauthorized because credentials are wrong)
        for (var i = 1; i <= 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // 6th attempt from the exact same IP must be rate limited to prevent brute force
        var rejectedResponse = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);

        // Verify standard Retry-After header and RFC 7807 problem details
        Assert.True(rejectedResponse.Headers.Contains("Retry-After"));
        Assert.Equal("60", rejectedResponse.Headers.GetValues("Retry-After").First());

        var content = await rejectedResponse.Content.ReadAsStringAsync();
        Assert.Contains("Too Many Requests", content);
        Assert.Contains("admin-login", content);

        // A different IP address should NOT be affected
        var otherClient = CreateClientForIp($"203.0.113.{Guid.NewGuid().ToString().Substring(0, 4)}");
        var otherResponse = await otherClient.PostAsJsonAsync("/api/auth/login", loginPayload);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, otherResponse.StatusCode);
    }

    [Fact]
    public async Task AdminLogin_WithValidCredentials_ReturnsAccessTokenAndProfile()
    {
        var ip = $"198.51.200.{Guid.NewGuid().ToString().Substring(0, 4)}";
        var client = CreateClientForIp(ip);

        var request = new LoginRequest(RateLimitingTestFactory.DefaultAdminEmail, RateLimitingTestFactory.DefaultAdminPassword);
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.AccessToken));
        Assert.Equal("bearer", loginResult.TokenType.ToLowerInvariant());
        Assert.NotNull(loginResult.User);
        Assert.Equal(RateLimitingTestFactory.DefaultAdminEmail, loginResult.User.Email);
    }

    [Fact]
    public async Task CommentSpam_Enforces3RequestsLimit_Rejects4thWith429()
    {
        var ip = $"198.51.101.{Guid.NewGuid().ToString().Substring(0, 4)}";
        var client = CreateClientForIp(ip);

        var testPostId = Guid.NewGuid();
        // Seed post into DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var author = await db.Users.FirstAsync();
            db.Posts.Add(new Post
            {
                Id = testPostId,
                Title = "Rate Limiting Post",
                Slug = $"rate-limiting-post-{Guid.NewGuid()}",
                Content = "Post content",
                AuthorId = author.Id,
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var commentPayload = new CreateGuestCommentRequest(
            Content: "Great insights on performance!",
            Email: "alice@example.com",
            DisplayName: "Alice"
        );

        // 3 requests allowed within 1 minute
        for (var i = 1; i <= 3; i++)
        {
            var response = await client.PostAsJsonAsync($"/api/posts/{testPostId}/comments", commentPayload);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // 4th request from same IP must return 429
        var rejected = await client.PostAsJsonAsync($"/api/posts/{testPostId}/comments", commentPayload);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        Assert.True(rejected.Headers.Contains("Retry-After"));
        var content = await rejected.Content.ReadAsStringAsync();
        Assert.Contains("comment-spam", content);
    }

    [Fact]
    public async Task ReactionSpam_Enforces15RequestsLimit_Rejects16thWith429()
    {
        var ip = $"198.51.102.{Guid.NewGuid().ToString().Substring(0, 4)}";
        var client = CreateClientForIp(ip);

        var testPostId = Guid.NewGuid();
        var slug = $"rate-limiting-like-{Guid.NewGuid()}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var author = await db.Users.FirstAsync();
            db.Posts.Add(new Post
            {
                Id = testPostId,
                Title = "Reaction Post",
                Slug = slug,
                Content = "Content",
                AuthorId = author.Id,
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 15 likes allowed within 1 minute
        for (var i = 1; i <= 15; i++)
        {
            var response = await client.PostAsync($"/api/posts/{slug}/analytics/like", null);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // 16th like attempt must return 429
        var rejected = await client.PostAsync($"/api/posts/{slug}/analytics/like", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        Assert.True(rejected.Headers.Contains("Retry-After"));
        var content = await rejected.Content.ReadAsStringAsync();
        Assert.Contains("reaction-spam", content);
    }

    [Fact]
    public async Task OtpSpam_Enforces3RequestsLimit_Rejects4thWith429()
    {
        var ip = $"198.51.103.{Guid.NewGuid().ToString().Substring(0, 4)}";
        var client = CreateClientForIp(ip);

        var otpPayload = new SendOtpEmailRequest(
            To: "victim@example.com",
            OtpCode: "123456",
            RecipientName: "Victim"
        );

        // 3 OTP requests allowed
        for (var i = 1; i <= 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/emails/otp/onboarding", otpPayload);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // 4th OTP request must be blocked
        var rejected = await client.PostAsJsonAsync("/api/emails/otp/onboarding", otpPayload);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        Assert.True(rejected.Headers.Contains("Retry-After"));
        var content = await rejected.Content.ReadAsStringAsync();
        Assert.Contains("otp-spam", content);
    }
}

public class RateLimitingTestFactory : TestWebApplicationFactory
{
    public const string DefaultAdminPassword = "AdminSecurePassword123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUTHOR_PASSWORD"] = DefaultAdminPassword,
                ["ADMIN_PASSWORD"] = DefaultAdminPassword
            });
        });

        builder.ConfigureServices(services =>
        {
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);

            services.AddSingleton<IEmailService, EmailTestFactory.MockEmailService>();
        });
    }
}
