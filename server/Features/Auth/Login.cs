using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using deblog.Server.Common.Data;
using deblog.Server.Common.Security.RateLimiting;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace deblog.Server.Features.Auth;

public static class LoginEndpoint
{
    public static RouteGroupBuilder MapLogin(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            AppDbContext db,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<LoginRequest> logger,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Email and password are required." });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users
                .Include(u => u.Information)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

            if (user != null)
            {
                if (user.IsDeleted)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Forbidden",
                        detail: "This account has been deleted."
                    );
                }

                if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Forbidden",
                        detail: "This account has been suspended or banned."
                    );
                }
            }

            var authorEmail = (config["AUTHOR_EMAIL"] ?? config["ADMIN_EMAIL"])?.Trim().ToLowerInvariant();
            var authorPassword = config["AUTHOR_PASSWORD"]?.Trim();

            // 1. Configured Author Credentials Verification (Immediate evaluation)
            if (!string.IsNullOrWhiteSpace(authorEmail) &&
                !string.IsNullOrWhiteSpace(authorPassword) &&
                string.Equals(normalizedEmail, authorEmail, StringComparison.OrdinalIgnoreCase) &&
                request.Password == authorPassword)
            {
                user ??= await EnsureFallbackUserAsync(db, normalizedEmail, ct);

                var jwtSecret = config["JWT_SECRET"]
                    ?? config["Jwt:Secret"]
                    ?? "development_fallback_jwt_secret_key_at_least_32_chars_long_12345";

                var token = GenerateLocalJwt(user, jwtSecret);
                var userProfile = UserHelpers.ToProfileDto(user);

                return Results.Ok(new LoginResponse(
                    AccessToken: token,
                    TokenType: "bearer",
                    ExpiresIn: 86400,
                    RefreshToken: null,
                    User: userProfile
                ));
            }

            // 2. Production Path: Authenticate with Supabase Auth API
            var supabaseUrl = config["SUPABASE_URL"];
            var anonKey = config["SUPABASE_ANON_KEY"] ?? config["SUPABASE_KEY"];
            if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(anonKey) && !supabaseUrl.Contains("placeholder"))
            {
                try
                {
                    var client = httpClientFactory.CreateClient("SupabaseAuth");
                    client.DefaultRequestHeaders.Remove("apikey");
                    client.DefaultRequestHeaders.Add("apikey", anonKey);

                    var endpointUrl = $"{supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password";
                    var authPayload = new
                    {
                        email = normalizedEmail,
                        password = request.Password
                    };

                    var response = await client.PostAsJsonAsync(endpointUrl, authPayload, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>(cancellationToken: ct);
                        if (json != null && !string.IsNullOrWhiteSpace(json.AccessToken))
                        {
                            user ??= await EnsureFallbackUserAsync(db, normalizedEmail, ct);

                            var userProfile = UserHelpers.ToProfileDto(user);
                            return Results.Ok(new LoginResponse(
                                AccessToken: json.AccessToken,
                                TokenType: json.TokenType ?? "bearer",
                                ExpiresIn: json.ExpiresIn,
                                RefreshToken: json.RefreshToken,
                                User: userProfile
                            ));
                        }
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return Results.Json(new { message = "Invalid email or password." }, statusCode: StatusCodes.Status401Unauthorized);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Supabase HTTP auth failed for {Email}.", normalizedEmail);
                }
            }

            return Results.Json(new { message = "Invalid email or password." }, statusCode: StatusCodes.Status401Unauthorized);
        })
        .WithName("AdminLogin")
        .WithSummary("Authenticates admin with Supabase and issues access token. Protected against brute force.")
        .RequireRateLimiting(RateLimitingPolicies.AdminLogin);

        return group;
    }

    private static string GenerateLocalJwt(User user, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim("sub", user.Id.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static async Task<User> EnsureFallbackUserAsync(AppDbContext db, string email, CancellationToken ct)
    {
        var existing = await db.Users.Include(u => u.Information).FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (existing != null) return existing;

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = email.Split('@')[0],
            Role = UserRoles.Admin,
            Status = UserStatus.Active
        };
        db.Users.Add(newUser);
        await db.SaveChangesAsync(ct);
        return newUser;
    }

    private sealed class SupabaseTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
