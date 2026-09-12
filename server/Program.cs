using deblog.Server.Common.Extensions;
using deblog.Server.Common.Middleware;
using deblog.Server.Common.Services;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Users;
using DotNetEnv;

// 1. Load environment variables from .env
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// 2. Exception Handling & Problem Details
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Database (EF Core + Npgsql)
builder.Services.AddDatabase(builder.Configuration);

// 4. Supabase Auth & JWT Bearer with AdminOnly policy
builder.Services.AddSupabaseAuthentication(builder.Configuration);

// 5. In-Memory Cache & Analytics Tracker
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAnalyticsTracker, MemoryAnalyticsTracker>();

// 6. Swagger / OpenAPI with JWT Bearer support
builder.Services.AddConfiguredSwagger();

// 7. CORS Configuration
var allowedOrigins = builder.Configuration["CORS_ORIGINS"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? new[] { "http://localhost:4200", "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DeblogCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// 8. Auto-seed Main Author profile on startup
await app.SeedMainAuthorAsync();

// 9. Request Pipeline Configuration
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ENABLE_SWAGGER", true))
{
    app.UseConfiguredSwagger();
}

app.UseCors("DeblogCorsPolicy");

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
app.MapUserEndpoints();
app.MapPostEndpoints();
app.MapCommentEndpoints();

app.Run();

public partial class Program { }
