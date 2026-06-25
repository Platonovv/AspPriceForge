using Microsoft.EntityFrameworkCore;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;
using PriceForge.Infrastructure.Persistence;

namespace PriceForge.IntegrationTests.ProductIntegrationTests;

[Trait("Category", "Integration")]
public class ProductPersistenceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ProductPersistenceTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SaveAndLoad_Product_RoundTripsWithMoney()
    {
        await _fixture.ResetAsync();

        DbContextOptions<AppDbContext> options
            = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_fixture.ConnectionString).Options;

        var product = new Product(new Sku("TEST-1"), "Тест", new Money(1234m, Currency.RUB));

        // Act — сохранить в ОДНОМ контексте
        await using (var db = new AppDbContext(options))
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        // Act — прочитать в НОВОМ контексте (без трекинга от прошлого)
        Product? loaded;
        await using (var db = new AppDbContext(options))
        {
            loaded = await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        }

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(1234m, loaded.BasePrice.Amount);
        Assert.Equal(Currency.RUB, loaded.BasePrice.Currency);
    }

    [Fact]
    public async Task Insert_Product_WithMoney()
    {
        await _fixture.ResetAsync();

        DbContextOptions<AppDbContext> options
            = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_fixture.ConnectionString).Options;

        var product = new Product(new Sku("TEST-1"), "Тест", new Money(1234m, Currency.RUB));

        // Act — сохранить в ОДНОМ контексте
        await using (var db = new AppDbContext(options))
        {
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();
        }

        // Act — прочитать в НОВОМ контексте (без трекинга от прошлого)
        Product? loaded;
        int count;
        await using (var db = new AppDbContext(options))
        {
            loaded = await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            count = await db.Products.CountAsync();
        }

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(1, count);
        Assert.Equal(1234m, loaded.BasePrice.Amount);
        Assert.Equal(Currency.RUB, loaded.BasePrice.Currency);
    }
}
