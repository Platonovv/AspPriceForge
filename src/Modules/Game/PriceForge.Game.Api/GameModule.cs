using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Game.Application;
using PriceForge.Game.Infrastructure;

namespace PriceForge.Game.Api;

public static class GameModule
{
    public static IServiceCollection AddGameModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGameApplication(configuration);
        services.AddGameInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(GameModule).Assembly);

        return services;
    }
}
