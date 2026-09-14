using deblog.Server.Common.Extensions;
using deblog.Server.Common.Middleware;
using deblog.Server.Common.Security.RateLimiting;
using deblog.Server.Common.Services;
using deblog.Server.Features.Analytics;
using deblog.Server.Features.Auth;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Emails;
using deblog.Server.Features.Media;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Settings;
using deblog.Server.Features.Users;
using DotNetEnv;
using Resend;

// 1. Load environment variables from .env
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Sentry Integration
var sentryDsn = builder.Configuration["SENTRY_DSN"]?.Trim();
if (!string.IsNullOrWhiteSpace(sentryDsn) && !sentryDsn.StartsWith("placeholder", StringComparison.OrdinalIgnoreCase))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.TracesSampleRate = 1.0;
        options.SendDefaultPii = false;
        options.AttachStacktrace = true;
        options.Environment = builder.Environment.EnvironmentName;
    });
}

// 2. Exception Handling, Problem Details & JSON Options
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: true));
});

// Configure Forwarded Headers for reverse proxies (Render / Cloudflare)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// 3. Database (EF Core + Npgsql)
builder.Services.AddDatabase(builder.Configuration);

// 4. Supabase Auth & JWT Bearer with AdminOnly policy
builder.Services.AddSupabaseAuthentication(builder.Configuration);

// 5. In-Memory Cache & Analytics Tracker & Cloudinary & Resend Email
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAnalyticsTracker, MemoryAnalyticsTracker>();
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

var resendApiKey = builder.Configuration["RESEND_API_KEY"]?.Trim();
if (!string.IsNullOrWhiteSpace(resendApiKey) && !resendApiKey.StartsWith("re_placeholder", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddResend(options => options.ApiToken = resendApiKey);
}
builder.Services.AddSingleton<IEmailService, ResendEmailService>();

builder.Services.AddScoped<IDataReconciliationService, DataReconciliationService>();
builder.Services.AddHostedService<DataSyncBackgroundService>();

builder.Services.AddHttpClient();
builder.Services.AddDeblogRateLimiting(builder.Configuration);

// 6. Swagger / OpenAPI with JWT Bearer support
builder.Services.AddConfiguredSwagger();

// 7. CORS Configuration
var allowedOrigins = builder.Configuration["CORS_ORIGINS"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(o => o.TrimEnd('/'))
    .ToArray()
    ?? new[] { "http://localhost:4200", "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DeblogCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            try
            {
                var uri = new Uri(origin);
                var host = uri.Host.ToLowerInvariant();

                // Local development origins
                if (host == "localhost" || host == "127.0.0.1") return true;

                // User's custom domain & subdomains (e.g. deblog.derickespinosa.site)
                if (host == "deblog.derickespinosa.site" || host.EndsWith(".derickespinosa.site")) return true;

                // Vercel deployment preview / production URLs (*.vercel.app)
                if (host.EndsWith(".vercel.app")) return true;

                // Explicitly configured origins from CORS_ORIGINS environment variable
                var cleanOrigin = origin.TrimEnd('/');
                return allowedOrigins.Any(o =>
                {
                    if (o.Contains('*'))
                    {
                        var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(o).Replace(@"\*", ".*") + "$";
                        return System.Text.RegularExpressions.Regex.IsMatch(cleanOrigin, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }
                    return string.Equals(o, cleanOrigin, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// 8. Auto-seed Main Author profile on startup
await app.SeedMainAuthorAsync();

// 9. Request Pipeline Configuration
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseRequestLogging();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ENABLE_SWAGGER", true))
{
    app.UseConfiguredSwagger();
}

app.UseCors("DeblogCorsPolicy");
app.UseRateLimiter();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// 10. Root Health & Status
app.MapGet("/", () => Results.Ok(new
{
    app = "deblog API",
    status = "healthy",
    framework = ".NET 10",
    docs = "/swagger"
}))
.WithTags("Health")
.WithName("HealthCheck");

// 11. Feature Vertical Slices
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPostEndpoints();
app.MapCommentEndpoints();
app.MapMediaEndpoints();
app.MapAnalyticsEndpoints();
app.MapSettingsEndpoints();
app.MapEmailEndpoints();

app.Run();

public partial class Program { }
