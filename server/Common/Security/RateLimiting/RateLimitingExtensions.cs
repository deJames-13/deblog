using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Sentry;

namespace deblog.Server.Common.Security.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddDeblogRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var clientIp = GetClientIp(httpContext);
                var endpoint = httpContext.GetEndpoint();

                // Determine policy from endpoint metadata if available
                var rateLimitMetadata = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
                var policyName = rateLimitMetadata?.PolicyName ?? "rate-limit";

                const int retryAfterSeconds = 60;
                httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                httpContext.Response.ContentType = "application/problem+json";

                var isLogin = string.Equals(policyName, RateLimitingPolicies.AdminLogin, StringComparison.OrdinalIgnoreCase);

                // Dispatch security event to Sentry
                try
                {
                    var sentryEvent = new SentryEvent
                    {
                        Message = $"[Security] Rate limit exceeded: '{policyName}' on {httpContext.Request.Path}",
                        Level = isLogin ? SentryLevel.Warning : SentryLevel.Info
                    };

                    sentryEvent.SetTag("security.incident", "rate_limit_exceeded");
                    sentryEvent.SetTag("security.policy", policyName);
                    sentryEvent.SetTag("client.ip", clientIp);
                    sentryEvent.SetTag("http.route", httpContext.Request.Path.Value ?? "/");
                    sentryEvent.SetTag("http.method", httpContext.Request.Method);
                    sentryEvent.SetTag("http.status_code", "429");

                    if (isLogin)
                    {
                        sentryEvent.SetTag("security.alert", "brute_force_suspect");
                    }

                    SentrySdk.CaptureEvent(sentryEvent);
                }
                catch
                {
                    // Fail-safe: Sentry logging failure must never disrupt client response
                }

                var problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = $"Rate limit quota exceeded for '{policyName}'. Please retry in {retryAfterSeconds} seconds.",
                    instance = httpContext.Request.Path.Value
                };

                await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
            };

            // 1. Admin Login policy: 5 requests per 5 minutes per IP (sliding window)
            options.AddPolicy(RateLimitingPolicies.AdminLogin, httpContext =>
            {
                var ip = GetClientIp(httpContext);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"login:{ip}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        SegmentsPerWindow = 5,
                        QueueLimit = 0
                    });
            });

            // 2. Comment Spam policy: 3 comments per 1 minute per IP (fixed window)
            options.AddPolicy(RateLimitingPolicies.CommentSpam, httpContext =>
            {
                var ip = GetClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"comment:{ip}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            // 3. Reaction Spam policy (Likes/Shares): 15 per 1 minute per IP (sliding window)
            options.AddPolicy(RateLimitingPolicies.ReactionSpam, httpContext =>
            {
                var ip = GetClientIp(httpContext);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"reaction:{ip}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0
                    });
            });

            // 4. OTP Spam policy: 3 requests per 5 minutes per IP (fixed window)
            options.AddPolicy(RateLimitingPolicies.OtpSpam, httpContext =>
            {
                var ip = GetClientIp(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"otp:{ip}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    });
            });

            // 5. Global Fallback policy: 100 requests per 1 minute per IP (sliding window)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ip = GetClientIp(httpContext);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"global:{ip}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 10
                    });
            });
        });

        return services;
    }

    public static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
        {
            var firstIp = forwarded.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstIp)) return firstIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
