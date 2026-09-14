using deblog.Server.Common.Data;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace deblog.Server.Tests.Common;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;
    public const string DefaultAdminEmail = "admin@deblog.dev";

    public TestWebApplicationFactory()
    {
        _databaseName = $"DeblogTestDb_{Guid.NewGuid()}";
        Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");
        Environment.SetEnvironmentVariable("IN_MEMORY_DB_NAME", _databaseName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["USE_IN_MEMORY_DB"] = "true",
                ["IN_MEMORY_DB_NAME"] = _databaseName,
                ["AUTHOR_EMAIL"] = DefaultAdminEmail,
                ["AUTHOR_USERNAME"] = "admin",
                ["AUTHOR_DISPLAYNAME"] = "Admin Author",
                ["APP_BASE_URL"] = "http://localhost:4200",
                ["DB_SCHEMA"] = "deblog"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace Authentication with TestAuthHandler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAnonymousClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}");
        return client;
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", DefaultAdminEmail);
        client.DefaultRequestHeaders.Add("X-Test-Role", UserRoles.Admin);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}");
        return client;
    }

    public HttpClient CreateUserClient(string email = "reader@example.com", string role = UserRoles.User)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}");
        return client;
    }

    public AppDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
