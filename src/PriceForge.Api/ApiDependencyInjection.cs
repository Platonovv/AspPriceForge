using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PriceForge.Api.Authentication;
using PriceForge.Api.Common;
using PriceForge.Api.Filters;
using PriceForge.Api.Middleware;

namespace PriceForge.Api;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LoginRateLimitSettings>()
                .Bind(configuration.GetSection("LoginRateLimit"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddExceptionHandler<GlobalExceptionHandler>().AddProblemDetails();
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("EnterLogin",
                              httpContext => RateLimitPartition.GetFixedWindowLimiter(
                                  GetClientPartitionKey(httpContext),
                                  _ =>
                                  {
                                      LoginRateLimitSettings loginRateLimit = httpContext.RequestServices
                                          .GetRequiredService<IOptions<LoginRateLimitSettings>>()
                                          .Value;

                                      return new FixedWindowRateLimiterOptions
                                      {
                                          PermitLimit = loginRateLimit.PermitLimit,
                                          Window = TimeSpan.FromMinutes(loginRateLimit.WindowMinutes)
                                      };
                                  }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        services.AddSignalR().AddStackExchangeRedis(configuration.GetConnectionString("Redis")!);

        services.AddScoped<IdempotencyFilter>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder().AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        return services;
    }

    private static string GetClientPartitionKey(HttpContext httpContext)
    {
        IPAddress? connectionRemoteIpAddress = httpContext.Connection.RemoteIpAddress;

        return connectionRemoteIpAddress == null            ? "unknown" :
               connectionRemoteIpAddress.IsIPv4MappedToIPv6 ? connectionRemoteIpAddress.MapToIPv4().ToString() :
                                                              connectionRemoteIpAddress.ToString();
    }
}
