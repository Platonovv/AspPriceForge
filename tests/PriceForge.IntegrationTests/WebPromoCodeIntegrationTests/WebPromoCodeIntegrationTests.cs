using System.Net;
using System.Net.Http.Json;
using PriceForge.Application.PromoCodes;
using PriceForge.Domain.PromoCodes;
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

    private async Task CreatePromoCodeAsync(string code, decimal percent, decimal minOrderAmount)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/promocodes",
                                                                     new
                                                                     {
                                                                         Code = code,
                                                                         DiscountPercent = percent,
                                                                         ValidFrom = DateTime.UtcNow.AddHours(-1),
                                                                         ValidTo = DateTime.UtcNow.AddHours(1),
                                                                         UsedLimit = 10,
                                                                         MinOrderAmount = minOrderAmount
                                                                     });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Quote_ValidCode_ReturnsDiscountedTotal()
    {
        await _factory.ResetAsync();

        await _client.RegisterAndImpersonateAsync("quote-valid@priceforge.test");
        Guid productId = await CreateProductAsync("QUOTE-1", 1000m);
        await CreatePromoCodeAsync("SALE25", 25m, 100m);

        await _client.PostAsJsonAsync("/api/v1/cart/items", new {ProductId = productId, Quantity = 1});

        HttpResponseMessage response
            = await _client.GetAsync("/api/v1/promocodes/quote?code=SALE25&region=Ru");
        response.EnsureSuccessStatusCode();

        QuotePromoCodeDto? quote = await response.Content.ReadFromJsonAsync<QuotePromoCodeDto>();

        Assert.NotNull(quote);
        Assert.Equal(750m, quote.TotalPrice);
    }

    [Fact]
    public async Task Quote_UnknownCode_ReturnsCartTotalWithReason()
    {
        await _factory.ResetAsync();

        await _client.RegisterAndImpersonateAsync("quote-unknown@priceforge.test");
        Guid productId = await CreateProductAsync("QUOTE-2", 1000m);

        await _client.PostAsJsonAsync("/api/v1/cart/items", new {ProductId = productId, Quantity = 1});

        HttpResponseMessage response
            = await _client.GetAsync("/api/v1/promocodes/quote?code=NOSUCHCODE&region=Ru");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        QuotePromoCodeDto? quote = await response.Content.ReadFromJsonAsync<QuotePromoCodeDto>();

        Assert.NotNull(quote);
        Assert.Equal(1000m, quote.TotalPrice);
        Assert.NotEqual(PromoCodeReason.None, quote.PromoCodeReason);
    }

    [Fact]
    public async Task Quote_BelowMinOrderAmount_IsNotApplied()
    {
        await _factory.ResetAsync();

        await _client.RegisterAndImpersonateAsync("quote-min@priceforge.test");
        Guid productId = await CreateProductAsync("QUOTE-3", 100m);
        await CreatePromoCodeAsync("BIGONLY", 25m, 5000m);

        await _client.PostAsJsonAsync("/api/v1/cart/items", new {ProductId = productId, Quantity = 1});

        HttpResponseMessage response
            = await _client.GetAsync("/api/v1/promocodes/quote?code=BIGONLY&region=Ru");
        response.EnsureSuccessStatusCode();

        QuotePromoCodeDto? quote = await response.Content.ReadFromJsonAsync<QuotePromoCodeDto>();

        Assert.NotNull(quote);
        Assert.Equal(100m, quote.TotalPrice);
        Assert.NotEqual(PromoCodeReason.None, quote.PromoCodeReason);
    }
}
