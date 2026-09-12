using Microsoft.OpenApi;

namespace deblog.Server.Common.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddConfiguredSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "deblog API",
                Version = "v1",
                Description = "ASP.NET Core Web API for deblog platform with Supabase Auth & PostgreSQL"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            var requirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            };

            options.AddSecurityRequirement(_ => requirement);

        });

        return services;
    }

    public static IApplicationBuilder UseConfiguredSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "deblog API v1");
            c.RoutePrefix = "swagger";
        });

        return app;
    }
}
