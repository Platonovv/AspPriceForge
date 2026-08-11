using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.BackgroundServices;
using PriceForge.Application.Behaviors;
using PriceForge.Application.Cache;
using PriceForge.Application.Caching;
using PriceForge.Application.Carts;
using PriceForge.Application.Common;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;

namespace PriceForge.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationDependencyInjection).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly);

            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<OutboxDispatcher>();

        services.AddScoped<IPricingStrategy, VipDiscountStrategy>();
        services.AddScoped<IPricingStrategy, VolumeStrategy>();
        services.AddScoped<PriceCalculator>();
        services.AddScoped<CartPricingService>();
        services.AddScoped<ProductPricingService>();
        services.AddScoped<TierService>();
        services.AddScoped<ICacheVersion, CacheVersion>();

        return services;
    }
}
