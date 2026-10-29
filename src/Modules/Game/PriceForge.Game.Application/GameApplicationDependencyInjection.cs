using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Contracts;

namespace PriceForge.Game.Application;

public static class GameApplicationDependencyInjection
{
    public static IServiceCollection AddGameApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(GameApplicationDependencyInjection).Assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GameApplicationDependencyInjection).Assembly));

        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<WalletSettings>()
                .Bind(configuration.GetSection("Wallet"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddScoped<IGameModuleApi, GameModuleApi>();

        return services;
    }
}
