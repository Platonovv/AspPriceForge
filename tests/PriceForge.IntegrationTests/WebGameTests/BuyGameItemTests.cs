using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Application.Purchases;
using PriceForge.Game.Domain.Purchases;
using PriceForge.Game.Domain.Wallets;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebGameTests;

[Trait("Category", "Integration")]
public class BuyGameItemTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public BuyGameItemTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Buy_SpendsCoinsAndSavesPurchaseWithJournalEntry()
    {
        Guid userId = await CreateUserWithCoinsAsync("buy-item-ok@priceforge.test", 200);

        CoinPurchaseDto result = await SendAsync(new BuyGameItemCommand(userId, GameTestData.SpeedBoostId, "buy-ok-1"));

        Assert.Equal(GameTestData.SpeedBoostId, result.ProductId);
        Assert.Equal(GameTestData.SpeedBoostPrice, result.CoinPrice);

        using IServiceScope scope = _factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        Wallet wallet = await db.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);
        CoinPurchase purchase = await db.CoinPurchases.AsNoTracking().SingleAsync(p => p.UserId == userId);
        List<WalletTransaction> entries = await db.WalletTransactions.AsNoTracking()
                                                  .Where(t => t.UserId == userId)
                                                  .ToListAsync();

        Assert.Equal(50, wallet.Balance);
        Assert.Equal(result.PurchaseId, purchase.Id);
        Assert.Contains(entries, t => t.Amount == -GameTestData.SpeedBoostPrice && t.Reason == WalletTransactionReason.Purchase);
    }

    [Fact]
    public async Task Buy_RepeatWithSameKey_ReturnsSamePurchaseWithoutSecondSpend()
    {
        Guid userId = await CreateUserWithCoinsAsync("buy-item-repeat@priceforge.test", 400);
        BuyGameItemCommand command = new(userId, GameTestData.SpeedBoostId, "buy-repeat-1");

        CoinPurchaseDto first = await SendAsync(command);
        CoinPurchaseDto second = await SendAsync(command);

        Assert.Equal(first.PurchaseId, second.PurchaseId);

        using IServiceScope scope = _factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        Wallet wallet = await db.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);
        int purchases = await db.CoinPurchases.AsNoTracking().CountAsync(p => p.UserId == userId);
        int spends = await db.WalletTransactions.AsNoTracking()
                             .CountAsync(t => t.UserId == userId && t.Reason == WalletTransactionReason.Purchase);

        Assert.Equal(250, wallet.Balance);
        Assert.Equal(1, purchases);
        Assert.Equal(1, spends);
    }

    [Fact]
    public async Task Buy_NotEnoughCoins_ThrowsDomainExceptionAndSavesNothing()
    {
        Guid userId = await CreateUserWithCoinsAsync("buy-item-poor@priceforge.test", 100);

        await Assert.ThrowsAsync<DomainException>(
            () => SendAsync(new BuyGameItemCommand(userId, GameTestData.GoldenSwordId, "buy-poor-1")));

        using IServiceScope scope = _factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        Wallet wallet = await db.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);

        Assert.Equal(100, wallet.Balance);
        Assert.False(await db.CoinPurchases.AsNoTracking().AnyAsync(p => p.UserId == userId));
    }

    [Fact]
    public async Task Buy_WalletNeverEarned_ThrowsDomainException()
    {
        await _factory.SeedGameItemsAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => SendAsync(new BuyGameItemCommand(Guid.NewGuid(), GameTestData.SpeedBoostId, "buy-no-wallet-1")));
    }

    [Fact]
    public async Task Buy_UnknownProduct_ThrowsNotFoundException()
    {
        Guid userId = await CreateUserWithCoinsAsync("buy-item-unknown@priceforge.test", 200);

        await Assert.ThrowsAsync<NotFoundException>(
            () => SendAsync(new BuyGameItemCommand(userId, Guid.NewGuid(), "buy-unknown-1")));
    }

    private async Task<CoinPurchaseDto> SendAsync(BuyGameItemCommand command)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        return await sender.Send(command, CancellationToken.None);
    }

    private async Task<Guid> CreateUserWithCoinsAsync(string email, long coins)
    {
        await _factory.SeedGameItemsAsync();

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        Guid userId = await client.RegisterAndImpersonateAsync(email);

        HttpResponseMessage response = await client.EarnAsync(coins);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return userId;
    }
}
