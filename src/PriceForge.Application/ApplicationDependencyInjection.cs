using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Authentication;
using PriceForge.Application.BackgroundServices;
using PriceForge.Application.Behaviors;
using PriceForge.Application.Cache;
using PriceForge.Application.Caching;
using PriceForge.Application.Carts;
using PriceForge.Application.Common;
using PriceForge.Application.Images;
using PriceForge.Application.Products;
using PriceForge.Application.Users;
using PriceForge.Application.Users.UserTokens;
using PriceForge.Catalog.Contracts;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;

namespace PriceForge.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationDependencyInjection).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly);

            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        services.AddOptions<ImageCleanupSettings>()
                .Bind(configuration.GetSection("ImageCleanup"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<PublicPriceRefresherSettings>()
                .Bind(configuration.GetSection("PublicPriceRefresherSettings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<EmailTokenSettings>()
                .Bind(configuration.GetSection("EmailSettings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<LoginAttemptSettings>()
                .Bind(configuration.GetSection("LoginAttemptSettings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);

        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<PublicPriceRefresherService>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<ImageCleanupService>();

        services.AddScoped<IPricingStrategy, VipDiscountStrategy>();
        services.AddScoped<IPricingStrategy, VolumeStrategy>();
        services.AddScoped<PriceCalculator>();
        services.AddScoped<CartPricingService>();
        services.AddScoped<ProductPricingService>();
        services.AddScoped<PublicPriceRecalculator>();
        services.AddScoped<TierService>();
        services.AddScoped<ICacheVersion, CacheVersion>();
        services.AddScoped<ImageCleaner>();
        services.AddScoped<PublicPriceRefresher>();
        services.AddScoped<ICatalogModuleApi, CatalogModuleApi>();

        return services;
    }
}
