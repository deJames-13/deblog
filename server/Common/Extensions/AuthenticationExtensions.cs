using System.Security.Claims;
using System.Text;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace deblog.Server.Common.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddSupabaseAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["JWT_SECRET"]
            ?? configuration["Jwt:Secret"]
            ?? "development_fallback_jwt_secret_key_at_least_32_chars_long_12345";

        var key = Encoding.UTF8.GetBytes(jwtSecret);

        var supabaseUrl = configuration["SUPABASE_URL"];
        if (string.IsNullOrWhiteSpace(supabaseUrl))
        {
            var dbConn = configuration["DB_CONNECTION"] ?? string.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(dbConn, @"Username=postgres\.([a-zA-Z0-9]+)");
            if (match.Success)
            {
                supabaseUrl = $"https://{match.Groups[1].Value}.supabase.co";
            }
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                if (!string.IsNullOrWhiteSpace(supabaseUrl))
                {
                    options.Authority = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
                    options.MetadataAddress = $"{supabaseUrl.TrimEnd('/')}/auth/v1/.well-known/openid-configuration";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                }
                else
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                }
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var adminEmail = configuration["AUTHOR_EMAIL"] ?? configuration["ADMIN_EMAIL"];
                    if (string.IsNullOrWhiteSpace(adminEmail))
                    {
                        return false;
                    }

                    var userEmail = context.User.GetUserEmail();
                    if (string.Equals(userEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                        ?? context.User.FindFirst("role")?.Value;

                    return string.Equals(roleClaim, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
                });
            });
        });

        return services;
    }

    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (Guid.TryParse(subClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal, IConfiguration configuration)
    {
        var adminEmail = configuration["AUTHOR_EMAIL"] ?? configuration["ADMIN_EMAIL"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var userEmail = principal.GetUserEmail();
            if (string.Equals(userEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }


        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value
            ?? principal.FindFirst("role")?.Value;

        return string.Equals(roleClaim, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
    }
}
