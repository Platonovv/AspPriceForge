using System.Net;
using System.Net.Http.Json;
using PriceForge.Application.Products;
using PriceForge.Domain.Pricing;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebUserTierIntegrationTests;

[Trait("Category", "Integration")]
public class WebUserTierIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebUserTierIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
    }

    private async Task<Guid> CreateProductAsync(string sku, decimal price)
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products",
                                                                     new
                                                                     {
                                                                         Sku = sku,
                                                                         Name = $"Product {sku}",
                                                                         Price = price,
                                                                         Currency = "RUB"
                                                                     });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<decimal> GetPriceAsync(Guid productId, string query)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/products/{productId}/price?{query}");
        response.EnsureSuccessStatusCode();

        MoneyDto? money = await response.Content.ReadFromJsonAsync<MoneyDto>();

        Assert.NotNull(money);

        return money.Amount;
    }

    [Fact]
    public async Task Price_IgnoresTierFromQuery()
    {
        await _factory.ResetAsync();

        await _client.RegisterAndImpersonateAsync("tier-query@priceforge.test");
        Guid productId = await CreateProductAsync("TIER-1", 1000m);

        decimal claimingVip = await GetPriceAsync(productId, "quantity=1&tier=Vip&region=Ru");

        Assert.Equal(1000m, claimingVip);
    }

    [Fact]
    public async Task Price_FollowsTierAssignedByAdmin()
    {
        await _factory.ResetAsync();

        Guid userId = await _client.RegisterAndImpersonateAsync("tier-admin@priceforge.test");
        Guid productId = await CreateProductAsync("TIER-2", 1000m);

        decimal beforeChange = await GetPriceAsync(productId, "quantity=1&region=Ru");

        Assert.Equal(1000m, beforeChange);

        HttpResponseMessage change = await _client.PostAsJsonAsync("/api/v1/users/change-tier",
                                                                    new
                                                                    {
                                                                        UserId = userId,
                                                                        Tier = (int)CustomerTier.Vip
                                                                    });

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        decimal afterChange = await GetPriceAsync(productId, "quantity=1&region=Ru");

        Assert.Equal(850m, afterChange);
    }

    [Fact]
    public async Task ChangeTier_ToNone_ReturnsBadRequest()
    {
        await _factory.ResetAsync();

        Guid userId = await _client.RegisterAndImpersonateAsync("tier-none@priceforge.test");

        HttpResponseMessage change = await _client.PostAsJsonAsync("/api/v1/users/change-tier",
                                                                    new
                                                                    {
                                                                        UserId = userId,
                                                                        Tier = (int)CustomerTier.None
                                                                    });

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
    }

    [Fact]
    public async Task ChangeTier_ForUnknownUser_ReturnsNotFound()
    {
        await _factory.ResetAsync();

        await _client.RegisterAndImpersonateAsync("tier-missing@priceforge.test");

        HttpResponseMessage change = await _client.PostAsJsonAsync("/api/v1/users/change-tier",
                                                                    new
                                                                    {
                                                                        UserId = Guid.NewGuid(),
                                                                        Tier = (int)CustomerTier.Vip
                                                                    });

        Assert.Equal(HttpStatusCode.NotFound, change.StatusCode);
    }
}
