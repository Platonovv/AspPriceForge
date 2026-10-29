using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Game.Application.Items;
using PriceForge.Game.Application.Purchases;
using PriceForge.Game.Application.Transactions;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.Game.Infrastructure.Persistence.Repositories;
using PriceForge.Game.Infrastructure.Transactions;

namespace PriceForge.Game.Infrastructure;

public static class GameInfrastructureDependencyInjection
{
    public static IServiceCollection AddGameInfrastructure(this IServiceCollection services,
                                                           IConfiguration configuration)
    {
        services.AddDbContext<GameDbContext>(options => options.UseNpgsql(
                                                 configuration.GetConnectionString("Postgres")!,
                                                 npgsql => npgsql.MigrationsHistoryTable(
                                                     GameDbContext.MigrationsHistoryTable,
                                                     GameDbContext.Schema)));

        services.AddScoped<IWalletRepository, EfWalletRepository>();
        services.AddScoped<IGameItemRepository, EfGameItemRepository>();
        services.AddScoped<ICoinPurchaseRepository, EfCoinPurchaseRepository>();
        services.AddScoped<IGameTransactionRunner, GameTransactionRunner>();

        return services;
    }
}
