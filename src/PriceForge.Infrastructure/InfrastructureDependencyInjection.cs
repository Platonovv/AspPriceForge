using System.Data.Common;
using Amazon.Runtime;
using Amazon.S3;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PriceForge.Application.Authentication;
using PriceForge.Application.Brokers;
using PriceForge.Application.Carts;
using PriceForge.Application.Categories;
using PriceForge.Application.Common;
using PriceForge.Application.DeviceTokens;
using PriceForge.Application.Images;
using PriceForge.Application.Notifications;
using PriceForge.Application.Orders;
using PriceForge.Application.Products;
using PriceForge.Application.PromoCodes;
using PriceForge.Application.Promotions;
using PriceForge.Application.Transactions;
using PriceForge.Application.Wallets;
using PriceForge.Infrastructure.Authentication;
using PriceForge.Infrastructure.Images;
using PriceForge.Infrastructure.Notifications;
using PriceForge.Infrastructure.Persistence;
using PriceForge.Infrastructure.Persistence.Brokers;
using PriceForge.Infrastructure.Persistence.Factory;
using PriceForge.Infrastructure.Persistence.Queries;
using PriceForge.Infrastructure.Persistence.Repositories;
using PriceForge.Infrastructure.Transactions;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace PriceForge.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string firebaseCredPath = configuration["Firebase:CredentialsPath"]!;
        if (FirebaseApp.DefaultInstance is null
            && !string.IsNullOrWhiteSpace(firebaseCredPath)
            && File.Exists(firebaseCredPath))
        {
            try
            {
                FirebaseApp.Create(new AppOptions {Credential = GoogleCredential.FromFile(firebaseCredPath)});
            }
            catch (ArgumentException)
            {
                // FirebaseApp уже создан параллельным построением хоста (тесты идут параллельно) — гонка, игнорируем.
            }
        }

        services.AddDbContext<AppDbContext>(options
                                                => options.UseNpgsql(configuration.GetConnectionString("Postgres")!));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")!;
        });

        services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection("Jwt"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        services.AddOptions<SmtpSettings>()
                .Bind(configuration.GetSection("Smtp"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        services.AddOptions<StorageSettings>()
                .Bind(configuration.GetSection("Storage"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IConnection>(_ =>
        {
            string rabbitUri = configuration.GetConnectionString("RabbitMq")!;
            var connectionFactory = new ConnectionFactory {Uri = new Uri(rabbitUri)};
            return connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            string redisConnectionString = configuration.GetConnectionString("Redis")!;
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
        }

        services.AddSingleton<IMongoDatabase>(_ =>
        {
            var client = new MongoClient(configuration.GetConnectionString("Mongo")!);
            return client.GetDatabase("priceforge");
        });

        services.AddSingleton<IProductEventStore, MongoEventStore>();
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            StorageSettings storageSettings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
            return new AmazonS3Client(new BasicAWSCredentials(storageSettings.AccessKey, storageSettings.SecretKey),
                                      new AmazonS3Config
                                      {
                                          ServiceURL = storageSettings.Endpoint, ForcePathStyle = true
                                      });
        });
        services.AddSingleton<IImageStorage, ImageStorage>();
        services.AddSingleton<IEmailRateLimiter, EmailRateLimiter>();
        services.AddSingleton<ILoginAttemptLimiter, LoginAttemptLimiter>();

        services.AddScoped<IDapperQuery, DapperQuery>();

        services.AddScoped<IIntegrationEventHandler, ProductCreatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler, ProductPriceChangedEventHandler>();
        services.AddScoped<IIntegrationEventHandler, SendConfirmEmailEventHandler>();
        services.AddScoped<IIntegrationEventHandler, ResetPasswordEventHandler>();

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationChannel, EmailChannel>();
        services.AddScoped<INotificationChannel, PushChannel>();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAccessTokenRevoker, AccessTokenRevoker>();

        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<RabbitMqConsumer>();

        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<IOutboxRepository, EfOutboxRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IDeviceTokenRepository, EfDeviceTokenRepository>();
        services.AddScoped<IPromotionRepository, EfPromotionRepository>();
        services.AddScoped<IPromoCodeRepository, EfPromoCodeRepository>();

        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IProductImageRepository, EfProductImageRepository>();

        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<ICategoryPromotionRepository, EfCategoryPromotionRepository>();

        services.AddScoped<ICartRepository, RedisCartRepository>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IUserTokenRepository, EfUserTokenRepository>();
        services.AddScoped<IWalletRepository, EfWalletRepository>();

        services.AddScoped<ITransactionRunner, TransactionRunner>();

        return services;
    }
}
