using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.BackgroundServices;
using PriceForge.Application.Behaviors;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;
using PriceCalculator = PriceForge.Domain.Pricing.Strategies.PriceCalculator;

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
        services.AddScoped<IPricingStrategy, PriceCalculator>();
        services.AddScoped<Domain.Pricing.PriceCalculator>();
        return services;
    }
}
