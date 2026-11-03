using System.Net;
using System.Net.Http.Json;
using PriceForge.Game.Application.Purchases;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebGameTests;

[Trait("Category", "Integration")]
public class GamePurchaseEndpointTests : IClassFixture<ApiFactory>
{
    private const string PurchasesUrl = "/api/v1/game/purchases";

    private readonly ApiFactory _factory;

    public GamePurchaseEndpointTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Purchase_Returns201WithPurchaseAndLocation()
    {
        HttpClient client = await CreateClientWithCoinsAsync("purchase-api-ok@priceforge.test", 200);

        HttpResponseMessage response = await PostPurchaseAsync(client, GameTestData.SpeedBoostId, "api-ok-1");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CoinPurchaseDto? purchase = await response.Content.ReadFromJsonAsync<CoinPurchaseDto>();
        Assert.NotNull(purchase);
        Assert.Equal(GameTestData.SpeedBoostId, purchase.ProductId);
        Assert.Equal(GameTestData.SpeedBoostPrice, purchase.CoinPrice);
        Assert.Equal($"{PurchasesUrl}/{purchase.PurchaseId}", response.Headers.Location?.OriginalString);
        Assert.Equal(50, await GetBalanceAsync(client));
    }

    [Fact]
    public async Task Purchase_RepeatWithSameKey_ReturnsSamePurchaseAndSpendsOnce()
    {
        HttpClient client = await CreateClientWithCoinsAsync("purchase-api-repeat@priceforge.test", 400);

        HttpResponseMessage first = await PostPurchaseAsync(client, GameTestData.SpeedBoostId, "api-repeat-1");
        HttpResponseMessage second = await PostPurchaseAsync(client, GameTestData.SpeedBoostId, "api-repeat-1");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        CoinPurchaseDto? firstPurchase = await first.Content.ReadFromJsonAsync<CoinPurchaseDto>();
        CoinPurchaseDto? secondPurchase = await second.Content.ReadFromJsonAsync<CoinPurchaseDto>();
        Assert.Equal(firstPurchase?.PurchaseId, secondPurchase?.PurchaseId);
        Assert.Equal(250, await GetBalanceAsync(client));
    }

    [Fact]
    public async Task Purchase_SameKeyWithDifferentBody_Returns422()
    {
        HttpClient client = await CreateClientWithCoinsAsync("purchase-api-reuse@priceforge.test", 400);

        HttpResponseMessage first = await PostPurchaseAsync(client, GameTestData.SpeedBoostId, "api-reuse-1");
        HttpResponseMessage second = await PostPurchaseAsync(client, GameTestData.GoldenSwordId, "api-reuse-1");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.Equal(250, await GetBalanceAsync(client));
    }

    [Fact]
    public async Task Purchase_WithoutIdempotencyKey_Returns400()
    {
        HttpClient client = await CreateClientWithCoinsAsync("purchase-api-nokey@priceforge.test", 200);

        HttpResponseMessage response = await client.PostAsJsonAsync(PurchasesUrl,
                                                                    new {ProductId = GameTestData.SpeedBoostId});

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(200, await GetBalanceAsync(client));
    }

    [Fact]
    public async Task Purchase_NotEnoughCoins_Returns400()
    {
        HttpClient client = await CreateClientWithCoinsAsync("purchase-api-poor@priceforge.test", 100);

        HttpResponseMessage response = await PostPurchaseAsync(client, GameTestData.GoldenSwordId, "api-poor-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100, await GetBalanceAsync(client));
    }

    [Fact]
    public async Task Purchase_UnknownProduct_Returns404()
    {
        HttpClient client = await CreateClientWithCoinsAsync("purchase-api-unknown@priceforge.test", 200);

        HttpResponseMessage response = await PostPurchaseAsync(client, Guid.NewGuid(), "api-unknown-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_Anonymous_Returns401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await PostPurchaseAsync(client, GameTestData.SpeedBoostId, "api-anon-1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostPurchaseAsync(HttpClient client, Guid productId, string key)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, PurchasesUrl);
        request.Headers.Add("Idempotency-Key", key);
        request.Content = JsonContent.Create(new {ProductId = productId});

        return await client.SendAsync(request);
    }

    private static async Task<long> GetBalanceAsync(HttpClient client)
    {
        return await client.GetFromJsonAsync<long>("/api/v1/wallet");
    }

    private async Task<HttpClient> CreateClientWithCoinsAsync(string email, long coins)
    {
        await _factory.SeedGameItemsAsync();

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        await client.RegisterAndImpersonateAsync(email);

        HttpResponseMessage response = await client.EarnAsync(coins);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return client;
    }
}
