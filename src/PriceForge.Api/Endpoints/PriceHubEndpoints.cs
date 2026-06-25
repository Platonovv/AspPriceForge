using PriceForge.Api.Hubs;

namespace PriceForge.Api.Endpoints;

public static class PriceHubEndpoints
{
    public static IEndpointRouteBuilder MapPriceHubEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHub<PriceHub>("/hubs/price");
        return app;
    }
}
