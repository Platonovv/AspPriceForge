using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Game.Application.Items;
using PriceForge.Game.Domain.Items;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebGameTests;

[Trait("Category", "Integration")]
public class GameItemsEndpointTests : IClassFixture<ApiFactory>
{
    private const string ItemsUrl = "/api/v1/game/items";

    private readonly ApiFactory _factory;

    public GameItemsEndpointTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetItems_ReturnsNamesAndPrices()
    {
        HttpClient client = await CreateAuthorizedClientAsync("items-api-ok@priceforge.test");

        List<GameItemDto> items = await GetItemsAsync(client);

        Assert.Contains(new GameItemDto(GameTestData.GoldenSwordId, "Golden Sword", 500), items);
        Assert.Contains(new GameItemDto(GameTestData.SpeedBoostId, "Speed Boost", GameTestData.SpeedBoostPrice), items);
    }

    [Fact]
    public async Task GetItems_AreSortedByPrice()
    {
        HttpClient client = await CreateAuthorizedClientAsync("items-api-sorted@priceforge.test");

        List<GameItemDto> items = await GetItemsAsync(client);

        Assert.Equal(items.OrderBy(item => item.CoinPrice).ToList(), items);
    }

    [Fact]
    public async Task GetItems_ItemWithoutCatalogProduct_IsSkipped()
    {
        HttpClient client = await CreateAuthorizedClientAsync("items-api-orphan@priceforge.test");
        Guid orphanProductId = await AddItemWithoutCatalogProductAsync();

        List<GameItemDto> items = await GetItemsAsync(client);

        Assert.DoesNotContain(items, item => item.ProductId == orphanProductId);
        Assert.Contains(items, item => item.ProductId == GameTestData.SpeedBoostId);
    }

    [Fact]
    public async Task GetItems_Anonymous_Returns401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(ItemsUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<List<GameItemDto>> GetItemsAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync(ItemsUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<GameItemDto>? items = await response.Content.ReadFromJsonAsync<List<GameItemDto>>();
        Assert.NotNull(items);

        return items;
    }

    private async Task<Guid> AddItemWithoutCatalogProductAsync()
    {
        Guid productId = Guid.NewGuid();

        using IServiceScope scope = _factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        db.GameItems.Add(new GameItem(productId, 1));
        await db.SaveChangesAsync();

        return productId;
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string email)
    {
        await _factory.SeedGameItemsAsync();

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        await client.RegisterAndImpersonateAsync(email);

        return client;
    }
}
