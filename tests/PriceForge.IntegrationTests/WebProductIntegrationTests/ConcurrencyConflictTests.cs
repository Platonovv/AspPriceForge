using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Application.Promotions;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class ConcurrencyConflictTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ConcurrencyConflictTests(ApiFactory factory) => _factory = factory;

    private async Task<Guid> SeedProductAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product(new Sku($"CNC-{Guid.NewGuid():N}"), "Concurrency", new Money(1000m, Currency.RUB));
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return product.Id;
    }

    private async Task ChangePriceAndSaveAsync(Guid productId, decimal newPrice)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        Product product = (await repository.GetByIdForUpdateAsync(productId, CancellationToken.None))!;
        product.ChangeBasePrice(new Money(newPrice, Currency.RUB));
        await repository.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StaleProductSave_ThroughProductRepository_IsConflict()
    {
        Guid productId = await SeedProductAsync();

        using IServiceScope staleScope = _factory.Services.CreateScope();
        var staleRepository = staleScope.ServiceProvider.GetRequiredService<IProductRepository>();
        Product stale = (await staleRepository.GetByIdForUpdateAsync(productId, CancellationToken.None))!;

        await ChangePriceAndSaveAsync(productId, 1200m);

        stale.ChangeBasePrice(new Money(900m, Currency.RUB));
        await Assert.ThrowsAsync<ConflictException>(() => staleRepository.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StaleProductSave_ThroughAnotherRepository_IsConflict()
    {
        Guid productId = await SeedProductAsync();

        using IServiceScope staleScope = _factory.Services.CreateScope();
        var staleProducts = staleScope.ServiceProvider.GetRequiredService<IProductRepository>();
        var stalePromotions = staleScope.ServiceProvider.GetRequiredService<IPromotionRepository>();
        Product stale = (await staleProducts.GetByIdForUpdateAsync(productId, CancellationToken.None))!;

        await ChangePriceAndSaveAsync(productId, 1200m);

        stale.ChangeBasePrice(new Money(900m, Currency.RUB));
        await Assert.ThrowsAsync<ConflictException>(() => stalePromotions.SaveChangesAsync(CancellationToken.None));
    }
}
