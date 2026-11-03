using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Application.Wallets;
using PriceForge.Game.Domain.Wallets;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebGameTests;

[Trait("Category", "Integration")]
public class WalletEarnIdempotencyTests : IClassFixture<ApiFactory>
{
    private const int MaxKeyLength = 64;
    private static readonly TimeSpan EnoughForOneCoin = TimeSpan.FromMilliseconds(200);

    private readonly ApiFactory _factory;

    public WalletEarnIdempotencyTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Repeat_WithSameKey_ReturnsBalanceAndCreditsOnce()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-repeat@priceforge.test");

        HttpResponseMessage first = await client.EarnAsync(100, "repeat-1");
        HttpResponseMessage second = await client.EarnAsync(100, "repeat-1");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(100, await second.Content.ReadFromJsonAsync<long>());
        Assert.Equal(100, await GetBalanceAsync(client));
        Assert.Equal(1, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task Repeat_AfterLaterEarn_ReturnsCurrentBalance()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-current@priceforge.test");

        await client.EarnAsync(100, "current-1");
        await Task.Delay(EnoughForOneCoin);
        await client.EarnAsync(1, "current-2");

        HttpResponseMessage repeat = await client.EarnAsync(100, "current-1");

        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(101, await repeat.Content.ReadFromJsonAsync<long>());
        Assert.Equal(2, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task DifferentKeys_CreditBoth()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-different@priceforge.test");

        await client.EarnAsync(100, "different-1");
        await Task.Delay(EnoughForOneCoin);
        HttpResponseMessage second = await client.EarnAsync(1, "different-2");

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(101, await GetBalanceAsync(client));
        Assert.Equal(2, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task SameKey_ForTwoUsers_CreditsBoth()
    {
        (HttpClient firstClient, _) = await CreateClientAsync("earn-key-user-1@priceforge.test");
        (HttpClient secondClient, _) = await CreateClientAsync("earn-key-user-2@priceforge.test");

        HttpResponseMessage first = await firstClient.EarnAsync(100, "shared-key");
        HttpResponseMessage second = await secondClient.EarnAsync(70, "shared-key");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(100, await GetBalanceAsync(firstClient));
        Assert.Equal(70, await GetBalanceAsync(secondClient));
    }

    [Fact]
    public async Task SameKey_WithDifferentAmount_Returns422()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-reuse@priceforge.test");

        await client.EarnAsync(100, "reuse-1");
        HttpResponseMessage second = await client.EarnAsync(50, "reuse-1");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.Equal(100, await GetBalanceAsync(client));
        Assert.Equal(1, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task WithoutKey_Returns400()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-missing@priceforge.test");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/wallet/earn", new {Coins = 100});

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task TooLongKey_Returns400()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-long@priceforge.test");

        HttpResponseMessage response = await client.EarnAsync(100, new string('k', MaxKeyLength + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task TwoSameRequestsAtOnce_OnNewWallet_CreditOnce()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-race-new@priceforge.test");

        await SendTwiceAndRetryAsync(client, 100, "race-new-1");

        Assert.Equal(100, await GetBalanceAsync(client));
        Assert.Equal(1, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task TwoSameRequestsAtOnce_OnExistingWallet_CreditOnce()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-race-old@priceforge.test");

        await client.EarnAsync(100, "race-old-1");
        await Task.Delay(EnoughForOneCoin);

        await SendTwiceAndRetryAsync(client, 1, "race-old-2");

        Assert.Equal(101, await GetBalanceAsync(client));
        Assert.Equal(2, await CountJournalEntriesAsync(userId));
    }

    [Fact]
    public async Task UniqueIndex_RejectsSameKeyPastHandlerCheck()
    {
        (HttpClient client, Guid userId) = await CreateClientAsync("earn-key-index@priceforge.test");
        await client.EarnAsync(100, "index-1");

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IWalletRepository repository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
            Wallet? wallet = await repository.GetByUserIdAsync(userId, CancellationToken.None);

            Assert.NotNull(wallet);
            wallet.Earn(1, 20, 30, "index-1", wallet.LastEarnedAtUtc.AddSeconds(10));

            await Assert.ThrowsAsync<ConflictException>(() => repository.SaveChangesAsync(CancellationToken.None));
        }

        Assert.Equal(100, await GetBalanceAsync(client));
        Assert.Equal(1, await CountJournalEntriesAsync(userId));
    }

    private static async Task SendTwiceAndRetryAsync(HttpClient client, long coins, string key)
    {
        HttpResponseMessage[] responses = await Task.WhenAll(client.EarnAsync(coins, key), client.EarnAsync(coins, key));

        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] {HttpStatusCode.OK, HttpStatusCode.Conflict}));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);

        HttpResponseMessage retry = await client.EarnAsync(coins, key);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    private static async Task<long> GetBalanceAsync(HttpClient client)
    {
        return await client.GetFromJsonAsync<long>("/api/v1/wallet");
    }

    private async Task<int> CountJournalEntriesAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        return await db.WalletTransactions.AsNoTracking().CountAsync(t => t.UserId == userId);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateClientAsync(string email)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        Guid userId = await client.RegisterAndImpersonateAsync(email);

        return (client, userId);
    }
}
