using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Application.Transactions;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Domain.Wallets;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebGameTests;

[Trait("Category", "Integration")]
public class WalletLockTests : IClassFixture<ApiFactory>
{
    private static readonly TimeSpan BlockedWindow = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private readonly ApiFactory _factory;

    public WalletLockTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LockedWallet_IsTrackedAndSpendIsSaved()
    {
        Guid userId = await CreateWalletWithCoinsAsync("wallet-lock-spend@priceforge.test");

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IWalletRepository repository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
            IGameTransactionRunner runner = scope.ServiceProvider.GetRequiredService<IGameTransactionRunner>();

            await runner.RunAsync(async () =>
            {
                Wallet? wallet = await repository.GetByUserIdForUpdateAsync(userId, CancellationToken.None);

                Assert.NotNull(wallet);
                wallet.Spend(30, DateTime.UtcNow);
                await repository.SaveChangesAsync(CancellationToken.None);
            }, CancellationToken.None);
        }

        using IServiceScope checkScope = _factory.Services.CreateScope();
        GameDbContext db = checkScope.ServiceProvider.GetRequiredService<GameDbContext>();

        Wallet saved = await db.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);
        List<WalletTransaction> entries = await db.WalletTransactions.AsNoTracking()
                                                  .Where(t => t.UserId == userId)
                                                  .ToListAsync();

        Assert.Equal(70, saved.Balance);
        Assert.Contains(entries, t => t.Amount == -30 && t.Reason == WalletTransactionReason.Purchase);
    }

    [Fact]
    public async Task LockedWallet_IsMissing_ReturnsNull()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IWalletRepository repository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
        IGameTransactionRunner runner = scope.ServiceProvider.GetRequiredService<IGameTransactionRunner>();

        Wallet? wallet = await runner.RunAsync(
            () => repository.GetByUserIdForUpdateAsync(Guid.NewGuid(), CancellationToken.None),
            CancellationToken.None);

        Assert.Null(wallet);
    }

    [Fact]
    public async Task SecondTransaction_WaitsUntilFirstReleasesLock()
    {
        Guid userId = await CreateWalletWithCoinsAsync("wallet-lock-wait@priceforge.test");

        TaskCompletionSource firstLocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task first = RunLockedAsync(userId, async () =>
        {
            firstLocked.SetResult();
            await releaseFirst.Task;
        });

        await firstLocked.Task.WaitAsync(TestTimeout);

        Task second = RunLockedAsync(userId, () => Task.CompletedTask);

        Task winner = await Task.WhenAny(second, Task.Delay(BlockedWindow));
        Assert.NotSame(second, winner);

        releaseFirst.SetResult();

        await first.WaitAsync(TestTimeout);
        await second.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task TwoEarnsFromSameVersion_SecondGetsConflictAndNothingIsLost()
    {
        Guid userId = await CreateWalletWithCoinsAsync("wallet-earn-race@priceforge.test");

        using IServiceScope firstScope = _factory.Services.CreateScope();
        using IServiceScope secondScope = _factory.Services.CreateScope();
        IWalletRepository firstRepository = firstScope.ServiceProvider.GetRequiredService<IWalletRepository>();
        IWalletRepository secondRepository = secondScope.ServiceProvider.GetRequiredService<IWalletRepository>();

        Wallet? firstWallet = await firstRepository.GetByUserIdAsync(userId, CancellationToken.None);
        Wallet? secondWallet = await secondRepository.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.NotNull(firstWallet);
        Assert.NotNull(secondWallet);
        EarnFifty(firstWallet);
        EarnFifty(secondWallet);

        await firstRepository.SaveChangesAsync(CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(
            () => secondRepository.SaveChangesAsync(CancellationToken.None));

        using IServiceScope checkScope = _factory.Services.CreateScope();
        GameDbContext db = checkScope.ServiceProvider.GetRequiredService<GameDbContext>();

        Wallet saved = await db.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);
        long journalSum = await db.WalletTransactions.AsNoTracking()
                                  .Where(t => t.UserId == userId)
                                  .SumAsync(t => t.Amount);

        Assert.Equal(150, saved.Balance);
        Assert.Equal(saved.Balance, journalSum);
    }

    [Fact]
    public async Task TwoFirstEarnsCreatingWallet_SecondGetsConflict()
    {
        Guid userId = Guid.NewGuid();
        DateTime createdAt = DateTime.UtcNow;

        using IServiceScope firstScope = _factory.Services.CreateScope();
        using IServiceScope secondScope = _factory.Services.CreateScope();
        IWalletRepository firstRepository = firstScope.ServiceProvider.GetRequiredService<IWalletRepository>();
        IWalletRepository secondRepository = secondScope.ServiceProvider.GetRequiredService<IWalletRepository>();

        await firstRepository.AddAsync(new Wallet(userId, createdAt), CancellationToken.None);
        await secondRepository.AddAsync(new Wallet(userId, createdAt), CancellationToken.None);

        await firstRepository.SaveChangesAsync(CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(
            () => secondRepository.SaveChangesAsync(CancellationToken.None));
    }

    private static void EarnFifty(Wallet wallet)
        => wallet.Earn(50, 20, 30, wallet.LastEarnedAtUtc.AddSeconds(10));

    private async Task RunLockedAsync(Guid userId, Func<Task> whileLocked)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IWalletRepository repository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
        IGameTransactionRunner runner = scope.ServiceProvider.GetRequiredService<IGameTransactionRunner>();

        await runner.RunAsync(async () =>
        {
            Wallet? wallet = await repository.GetByUserIdForUpdateAsync(userId, CancellationToken.None);

            Assert.NotNull(wallet);
            await whileLocked();
        }, CancellationToken.None);
    }

    private async Task<Guid> CreateWalletWithCoinsAsync(string email)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        Guid userId = await client.RegisterAndImpersonateAsync(email);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/wallet/earn", new {EarnMoney = 100});
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return userId;
    }
}
