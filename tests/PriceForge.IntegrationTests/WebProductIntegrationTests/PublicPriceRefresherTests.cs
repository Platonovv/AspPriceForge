using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Products;
using PriceForge.Domain.Categories;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class PublicPriceRefresherTests : IClassFixture<ApiFactory>
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateTime WindowFrom = Now.AddHours(-1);

    private readonly ApiFactory _factory;

    public PublicPriceRefresherTests(ApiFactory factory) => _factory = factory;

    private async Task<Guid> SeedProductAsync(decimal basePrice, decimal publicPrice)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product(new Sku($"REF-{Guid.NewGuid():N}"), "Refresh", new Money(basePrice, Currency.RUB));
        product.UpdatePublicPrice(publicPrice);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return product.Id;
    }

    private async Task SeedPromotionAsync(Guid productId, decimal percent, DateTime startAt, DateTime endAt)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<Promotion>().Add(new Promotion(productId, percent, startAt, endAt));
        await db.SaveChangesAsync();
    }

    private async Task SeedCategoryPromotionAsync(Guid productId, decimal percent, DateTime startAt, DateTime endAt)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new Category($"Refresh {Guid.NewGuid():N}");
        db.Categories.Add(category);
        db.ProductCategories.Add(new ProductCategory(productId, category.Id));
        db.CategoryPromotions.Add(new CategoryPromotion(category.Id, percent, startAt, endAt));
        await db.SaveChangesAsync();
    }

    private async Task<decimal> ReadPublicPriceAsync(Guid productId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Products.Where(p => p.Id == productId).Select(p => p.PublicPrice).SingleAsync();
    }

    private async Task RunOnceAsync(int batchSize = 500)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var refresher = scope.ServiceProvider.GetRequiredService<PublicPriceRefresher>();

        await refresher.RunOnceAsync(WindowFrom, Now, batchSize, CancellationToken.None);
    }

    [Fact]
    public async Task PromotionEndedInWindow_PublicPriceReturnsToBase()
    {
        Guid productId = await SeedProductAsync(5500m, 4675m);
        await SeedPromotionAsync(productId, 15m, Now.AddDays(-3), Now.AddMinutes(-30));

        await RunOnceAsync();

        Assert.Equal(5500m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task PromotionStartedInWindow_PublicPriceIsDiscounted()
    {
        Guid productId = await SeedProductAsync(5500m, 5500m);
        await SeedPromotionAsync(productId, 15m, Now.AddMinutes(-10), Now.AddDays(3));

        await RunOnceAsync();

        Assert.Equal(4675m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task CategoryPromotionStartedInWindow_ProductInCategoryIsDiscounted()
    {
        Guid productId = await SeedProductAsync(3000m, 3000m);
        await SeedCategoryPromotionAsync(productId, 20m, Now.AddMinutes(-10), Now.AddDays(3));

        await RunOnceAsync();

        Assert.Equal(2400m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task PromotionWithoutBoundaryInWindow_ProductIsNotTouched()
    {
        Guid productId = await SeedProductAsync(5500m, 1m);
        await SeedPromotionAsync(productId, 15m, Now.AddDays(-3), Now.AddDays(3));

        await RunOnceAsync();

        Assert.Equal(1m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task ManyProducts_AreRecalculatedAcrossSeveralBatches()
    {
        var productIds = new List<Guid>();

        for (int i = 0; i < 5; i++)
        {
            Guid productId = await SeedProductAsync(1000m, 1000m);
            await SeedPromotionAsync(productId, 10m, Now.AddMinutes(-10), Now.AddDays(3));
            productIds.Add(productId);
        }

        await RunOnceAsync(batchSize: 2);

        foreach (Guid productId in productIds)
        {
            Assert.Equal(900m, await ReadPublicPriceAsync(productId));
        }
    }
}
