using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using PriceForge.Api.Authentication;
using PriceForge.Api.Filters;
using PriceForge.Api.Middleware;

namespace PriceForge.Api;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>().AddProblemDetails();
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("EnterLogin",
                                          limiterOptions =>
                                          {
                                              limiterOptions.PermitLimit = 7;
                                              limiterOptions.Window = TimeSpan.FromMinutes(5);
                                          });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        services.AddSignalR().AddStackExchangeRedis(configuration.GetConnectionString("Redis")!);

        services.AddScoped<IdempotencyFilter>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        return services;
    }
}
