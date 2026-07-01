using Microsoft.EntityFrameworkCore;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;
using PriceForge.Infrastructure.Persistence;

namespace PriceForge.IntegrationTests.ProductIntegrationTests;

[Trait("Category", "Integration")]
public class PromotionPersistenceTest : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PromotionPersistenceTest(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateAndApplyPromotion()
    {
        await _fixture.ResetAsync();

        DbContextOptions<AppDbContext> options
            = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_fixture.ConnectionString).Options;

        var product = new Product(new Sku("TEST-1"), "Тест", new Money(1000m, Currency.RUB));
        var promotion = new Promotion(product.Id, 25m, DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

        // Act — сохранить в ОДНОМ контексте
        await using (var db = new AppDbContext(options))
        {
            db.Products.Add(product);
            db.Promotions.Add(promotion);
            await db.SaveChangesAsync();
        }

        // Act — прочитать в НОВОМ контексте (без трекинга от прошлого)
        Product? loaded;
        Promotion? promotionloaded;
        await using (var db = new AppDbContext(options))
        {
            loaded = await db.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            promotionloaded = await db.Promotions.FirstOrDefaultAsync(p => p.ProductId == product.Id);
        }

        var fakeStrategy = new IPricingStrategy[] {new VolumeStrategy()};
        var calculator = new PriceCalculator(fakeStrategy);

        // Assert
        Assert.NotNull(promotionloaded);

        Money promotionMoney = calculator.CalculatePromotion(loaded!.BasePrice, promotionloaded);

        // Assert
        Assert.Equal(750m, promotionMoney.Amount);
        Assert.Equal(25m, promotionloaded.Percent);

        Assert.Equal(promotion.StartAt, promotionloaded.StartAt, TimeSpan.FromSeconds(1));
        Assert.Equal(promotion.EndAt, promotionloaded.EndAt, TimeSpan.FromSeconds(1));

        Assert.True(promotionloaded.IsActiveAt(DateTime.UtcNow.AddHours(1)));
        Assert.False(promotionloaded.IsActiveAt(DateTime.UtcNow.AddHours(3)));
        Assert.False(promotionloaded.IsActiveAt(DateTime.UtcNow.AddHours(-1)));
    }
}
