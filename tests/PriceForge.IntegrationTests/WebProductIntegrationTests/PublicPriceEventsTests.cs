using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Domain.Promotions;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class PublicPriceEventsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public PublicPriceEventsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<Guid> CreateProductAsync(decimal price)
        => _client.CreateProductAsync($"EVT-{Guid.NewGuid():N}", "Public price event", price);

    private async Task AddActivePromotionAsync(Guid productId, decimal percent)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<Promotion>()
          .Add(new Promotion(productId, percent, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1)));
        await db.SaveChangesAsync();
    }

    private async Task<decimal> ReadPublicPriceAsync(Guid productId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Products.Where(p => p.Id == productId).Select(p => p.PublicPrice).SingleAsync();
    }

    private async Task ChangeBasePriceAsync(Guid productId, decimal newPrice)
    {
        HttpRequestMessage request = ProductRequests.AsRole(HttpMethod.Put,
                                                            $"/api/v1/products/{productId}/price",
                                                            "Admin",
                                                            new {NewPrice = newPrice, Currency = "RUB"});

        HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> PostAsAdminAsync(string url, object body)
        => await _client.SendAsync(ProductRequests.AsRole(HttpMethod.Post, url, "Admin", body));

    private async Task<Guid> CreateCategoryAsync()
    {
        HttpResponseMessage response = await PostAsAdminAsync("/api/v1/categories", new {Name = $"Cat {Guid.NewGuid():N}"});
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task CreateCategoryPromotionAsync(Guid categoryId, decimal percent)
    {
        HttpResponseMessage response = await PostAsAdminAsync($"/api/v1/categories/{categoryId}/promotions",
                                                              new
                                                              {
                                                                  Percent = percent,
                                                                  StartAt = DateTime.UtcNow.AddMinutes(-1),
                                                                  EndAt = DateTime.UtcNow.AddHours(1)
                                                              });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task AssignToCategoryAsync(Guid productId, Guid categoryId)
    {
        HttpResponseMessage response = await PostAsAdminAsync($"/api/v1/products/{productId}/categories",
                                                              new {CategoryId = categoryId});
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeBasePrice_WithActivePromotion_RecalculatesDiscountedPublicPrice()
    {
        Guid productId = await CreateProductAsync(5500m);
        await AddActivePromotionAsync(productId, 15m);

        await ChangeBasePriceAsync(productId, 6000m);

        Assert.Equal(5100m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task ChangeBasePrice_LoweredWithoutPromotion_PublicPriceFollowsBase()
    {
        Guid productId = await CreateProductAsync(5500m);

        await ChangeBasePriceAsync(productId, 5000m);

        Assert.Equal(5000m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task CreateProductPromotion_Active_RecalculatesPublicPrice()
    {
        Guid productId = await CreateProductAsync(5500m);

        HttpResponseMessage response = await PostAsAdminAsync("/api/v1/promotions",
                                                              new
                                                              {
                                                                  ProductId = productId,
                                                                  Percent = 15m,
                                                                  StartAt = DateTime.UtcNow.AddMinutes(-1),
                                                                  EndAt = DateTime.UtcNow.AddHours(1)
                                                              });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.Equal(4675m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task AssignToCategory_WithActiveCategoryPromotion_RecalculatesPublicPrice()
    {
        Guid categoryId = await CreateCategoryAsync();
        await CreateCategoryPromotionAsync(categoryId, 20m);
        Guid productId = await CreateProductAsync(3000m);

        await AssignToCategoryAsync(productId, categoryId);

        Assert.Equal(2400m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task CreateCategoryPromotion_RecalculatesEveryProductInCategory()
    {
        Guid categoryId = await CreateCategoryAsync();
        Guid first = await CreateProductAsync(3000m);
        Guid second = await CreateProductAsync(1000m);
        await AssignToCategoryAsync(first, categoryId);
        await AssignToCategoryAsync(second, categoryId);

        await CreateCategoryPromotionAsync(categoryId, 20m);

        Assert.Equal(2400m, await ReadPublicPriceAsync(first));
        Assert.Equal(800m, await ReadPublicPriceAsync(second));
    }
}
