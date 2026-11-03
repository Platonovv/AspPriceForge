using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Game.Domain.Wallets;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebGameTests;

[Trait("Category", "Integration")]
public class WalletJournalTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public WalletJournalTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Earn_WritesJournalEntryThatMatchesBalance()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        Guid userId = await client.RegisterAndImpersonateAsync("wallet-journal@priceforge.test");

        HttpResponseMessage response = await client.EarnAsync(100);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        List<WalletTransaction> entries = await db.WalletTransactions.AsNoTracking()
                                                  .Where(t => t.UserId == userId)
                                                  .ToListAsync();
        Wallet wallet = await db.Wallets.AsNoTracking().SingleAsync(w => w.UserId == userId);

        WalletTransaction entry = Assert.Single(entries);
        Assert.Equal(100, entry.Amount);
        Assert.Equal(WalletTransactionReason.Earn, entry.Reason);
        Assert.Equal(wallet.Balance, entries.Sum(t => t.Amount));
    }
}
