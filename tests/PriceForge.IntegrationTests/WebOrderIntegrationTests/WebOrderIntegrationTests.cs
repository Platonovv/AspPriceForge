using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Domain.Orders;
using PriceForge.Domain.PromoCodes;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebOrderIntegrationTests;

[Trait("Category", "Integration")]
public class WebOrderIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebOrderIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
    }

    private async Task<Guid> RegisterAndImpersonateAsync(string email)
    {
        HttpResponseMessage register
            = await _client.PostAsJsonAsync("/api/v1/auth/register", new {Email = email, Password = "Passw0rd!"});
        register.EnsureSuccessStatusCode();

        Guid userId = await register.Content.ReadFromJsonAsync<Guid>();

        _client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        return userId;
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

    [Fact]
    public async Task PlaceOrder_FixesPricesAndClearsCart()
    {
        await _factory.ResetAsync();

        Guid userId = await RegisterAndImpersonateAsync("order@priceforge.test");
        Guid productId = await CreateProductAsync("ORD-1", 1000m);

        HttpResponseMessage add
            = await _client.PostAsJsonAsync("/api/v1/cart/items", new {ProductId = productId, Quantity = 2});
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        HttpResponseMessage cartBefore = await _client.GetAsync("/api/v1/cart?tier=Regular&region=Ru");
        cartBefore.EnsureSuccessStatusCode();

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage place
            = await _client.PostAsync("/api/v1/orders?tier=Regular&region=Ru", null);
        Assert.Equal(HttpStatusCode.Created, place.StatusCode);

        Guid orderId = await place.Content.ReadFromJsonAsync<Guid>();

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Order? order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.NotNull(order);
        Assert.Equal(userId, order.UserId);
        Assert.Equal(2000m, order.ItemsTotal);
        Assert.Equal(2000m, order.Total);
        Assert.Single(order.OrderItems);
        Assert.Equal(productId, order.OrderItems[0].ProductId);
        Assert.Equal(2, order.OrderItems[0].Quantity);
        Assert.Equal(1000m, order.OrderItems[0].UnitPrice);

        HttpResponseMessage cartAfter = await _client.GetAsync("/api/v1/cart?tier=Regular&region=Ru");
        cartAfter.EnsureSuccessStatusCode();

        string body = await cartAfter.Content.ReadAsStringAsync();
        Assert.DoesNotContain(productId.ToString(), body);
    }

    [Fact]
    public async Task Quote_DoesNotBurnLimit_ButPlaceOrderDoes()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("promo-order@priceforge.test");
        Guid productId = await CreateProductAsync("ORD-2", 1000m);

        HttpResponseMessage createPromo = await _client.PostAsJsonAsync("/api/v1/promocodes",
                                                                        new
                                                                        {
                                                                            Code = "SALE25",
                                                                            DiscountPercent = 25m,
                                                                            ValidFrom = DateTime.UtcNow.AddHours(-1),
                                                                            ValidTo = DateTime.UtcNow.AddHours(1),
                                                                            UsedLimit = 10,
                                                                            MinOrderAmount = 100m
                                                                        });
        Assert.Equal(HttpStatusCode.Created, createPromo.StatusCode);

        await _client.PostAsJsonAsync("/api/v1/cart/items", new {ProductId = productId, Quantity = 1});

        for (var i = 0; i < 3; i++)
        {
            HttpResponseMessage quote
                = await _client.GetAsync("/api/v1/promocodes/quote?code=SALE25&tier=Regular&region=Ru");
            quote.EnsureSuccessStatusCode();
        }

        using (IServiceScope quoteScope = _factory.Services.CreateScope())
        {
            AppDbContext quoteDb = quoteScope.ServiceProvider.GetRequiredService<AppDbContext>();
            PromoCode? afterQuotes = await quoteDb.PromoCodes.FirstOrDefaultAsync();

            Assert.NotNull(afterQuotes);
            Assert.Equal(0, afterQuotes.UsedCount);
        }

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage place
            = await _client.PostAsync("/api/v1/orders?promoCode=SALE25&tier=Regular&region=Ru", null);
        Assert.Equal(HttpStatusCode.Created, place.StatusCode);

        Guid orderId = await place.Content.ReadFromJsonAsync<Guid>();

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        PromoCode? promo = await db.PromoCodes.FirstOrDefaultAsync();
        Assert.NotNull(promo);
        Assert.Equal(1, promo.UsedCount);

        Order? order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(order);
        Assert.Equal(1000m, order.ItemsTotal);
        Assert.Equal(750m, order.Total);
        Assert.Equal(250m, order.TotalDiscountAmount);
        Assert.Equal("SALE25", order.PromoCodeName);
    }

    [Fact]
    public async Task PlaceOrder_WithEmptyCart_Fails()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("empty-order@priceforge.test");

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage place = await _client.PostAsync("/api/v1/orders?tier=Regular&region=Ru", null);

        Assert.Equal(HttpStatusCode.BadRequest, place.StatusCode);
    }
}
