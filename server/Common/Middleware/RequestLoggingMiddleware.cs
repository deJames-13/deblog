using System.Diagnostics;

namespace deblog.Server.Common.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _isEnabled;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IWebHostEnvironment env, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _isEnabled = env.IsDevelopment() || config.GetValue<bool>("ENABLE_REQUEST_LOGGING", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_isEnabled)
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method.ToUpperInvariant();
        var path = context.Request.Path.Value ?? "/";
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        var feature = ResolveFeature(path);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[REQ][{Feature}][{Method}] {Path}{Query}", feature, method, path, query);

        try
        {
            await _next(context);
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "[ERR][{Feature}][{Method}] {Path}{Query} - Status {StatusCode} ({Elapsed}ms)",
                    feature, method, path, query, statusCode, elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "[RES][{Feature}][{Method}] {Path}{Query} - Status {StatusCode} OK ({Elapsed}ms)",
                    feature, method, path, query, statusCode, elapsedMs);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERR][{Feature}][{Method}] {Path}{Query} - Unhandled Exception after {Elapsed}ms: {Message}",
                feature, method, path, query, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private static string ResolveFeature(string path)
    {
        var lower = path.ToLowerInvariant();

        if (lower.Contains("/comments"))
            return "COMMENTS";

        if (lower.Contains("/posts"))
            return "POSTS";

        if (lower.Contains("/users"))
            return "USERS";

        if (lower.Contains("/analytics"))
            return "ANALYTICS";

        if (lower.Contains("/swagger"))
            return "SWAGGER";

        if (lower == "/" || lower.Contains("/health"))
            return "HEALTH";

        return "SYSTEM";
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
