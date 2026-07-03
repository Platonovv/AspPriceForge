using System.Net;
using System.Net.Http.Json;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebPromoCodeIntegrationTests;

[Trait("Category", "Integration")]
public class WebPromoCodeIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebPromoCodeIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateThenApply_ReturnsDiscountedAmount()
    {
        await _factory.ResetAsync();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true"); // → Admin


        var create = new
        {
            Code = "SALE25",
            DiscountPercent = 25m,
            ValidFrom = DateTime.UtcNow.AddHours(-1),
            ValidTo = DateTime.UtcNow.AddHours(1),
            UsedLimit = 10,
            MinOrderAmount = 100m
        };
        HttpResponseMessage createResp = await _client.PostAsJsonAsync("/api/v1/promocodes", create);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);


        var apply = new { Code = "SALE25", OrderMoney = 1000m };
        HttpResponseMessage applyResp = await _client.PostAsJsonAsync("/api/v1/promocodes/apply", apply);
        applyResp.EnsureSuccessStatusCode();

        decimal discounted = await applyResp.Content.ReadFromJsonAsync<decimal>();
        Assert.Equal(750m, discounted);
    }
}
