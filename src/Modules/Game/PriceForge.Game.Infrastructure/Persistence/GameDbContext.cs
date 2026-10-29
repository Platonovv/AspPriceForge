using Microsoft.EntityFrameworkCore;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Domain.Items;
using PriceForge.Game.Domain.Purchases;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Infrastructure.Persistence;

public sealed class GameDbContext : DbContext
{
    public const string Schema = "game";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<GameItem> GameItems => Set<GameItem>();
    public DbSet<CoinPurchase> CoinPurchases => Set<CoinPurchase>();

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
                                                     CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This item was updated by another action. Please refresh and try again.");
        }
    }
}
