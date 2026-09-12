using System.Security.Claims;
using System.Text.Encodings.Web;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace deblog.Server.Tests.Common;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If test header X-Test-Email is not present, treat as unauthenticated
        if (!Request.Headers.TryGetValue("X-Test-Email", out var emailValues) ||
            string.IsNullOrWhiteSpace(emailValues.FirstOrDefault()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = emailValues.First()!;
        var role = Request.Headers.TryGetValue("X-Test-Role", out var roleValues)
            ? roleValues.FirstOrDefault() ?? UserRoles.User
            : UserRoles.User;

        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var idValues) &&
                     Guid.TryParse(idValues.FirstOrDefault(), out var parsedId)
            ? parsedId
            : Guid.NewGuid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new(ClaimTypes.Email, email),
            new("email", email),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
