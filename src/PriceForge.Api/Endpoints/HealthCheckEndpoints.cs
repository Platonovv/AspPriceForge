using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace PriceForge.Api.Endpoints;

public static class HealthCheckEndpoints
{
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions {Predicate = _ => false});
        app.MapHealthChecks("/health/ready",
                            new HealthCheckOptions {Predicate = check => check.Tags.Contains("ready")});
        return app;
    }
}
