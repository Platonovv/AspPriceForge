using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Products;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class PublicPriceRecalculatorTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PublicPriceRecalculatorTests(ApiFactory factory) => _factory = factory;

    private async Task<Guid> SeedProductAsync(decimal basePrice, decimal? promotionPercent)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product(new Sku($"PUB-{Guid.NewGuid():N}"), "Public price", new Money(basePrice, Currency.RUB));
        db.Products.Add(product);

        if (promotionPercent is { } percent)
        {
            db.Set<Promotion>()
              .Add(new Promotion(product.Id, percent, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1)));
        }

        await db.SaveChangesAsync();

        return product.Id;
    }

    private async Task<decimal> ReadPublicPriceAsync(Guid productId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Products.Where(p => p.Id == productId).Select(p => p.PublicPrice).SingleAsync();
    }

    private async Task<decimal> RecalculateAndSaveAsync(Guid productId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var recalculator = scope.ServiceProvider.GetRequiredService<PublicPriceRecalculator>();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await recalculator.RecalculateAsync([productId], CancellationToken.None);

        decimal inMemory = db.ChangeTracker.Entries<Product>().Single(e => e.Entity.Id == productId).Entity.PublicPrice;

        await db.SaveChangesAsync();

        return inMemory;
    }

    [Fact]
    public async Task Recalculate_ProductWithActivePromotion_StoresDiscountedPrice()
    {
        Guid productId = await SeedProductAsync(5500m, 15m);

        await RecalculateAndSaveAsync(productId);

        Assert.Equal(4675m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task Recalculate_ProductWithoutPromotion_KeepsBasePrice()
    {
        Guid productId = await SeedProductAsync(1000m, null);

        await RecalculateAndSaveAsync(productId);

        Assert.Equal(1000m, await ReadPublicPriceAsync(productId));
    }

    [Fact]
    public async Task Recalculate_MidpointResult_IsRoundedAwayFromZeroInCode()
    {
        Guid productId = await SeedProductAsync(1.25m, 50m);

        decimal inMemory = await RecalculateAndSaveAsync(productId);

        Assert.Equal(0.63m, inMemory);
        Assert.Equal(0.63m, await ReadPublicPriceAsync(productId));
    }
}
