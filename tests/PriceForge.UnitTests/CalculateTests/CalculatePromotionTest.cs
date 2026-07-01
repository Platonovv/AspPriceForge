using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;
using PriceForge.Domain.Promotions;

namespace PriceForge.UnitTests.CalculateTests;

[Trait("Category", "Unit")]
public class CalculatePromotionTest
{
    [Theory]
    [InlineData(25, 750)]
    [InlineData(20, 800)]
    public void CalculatePromotion(decimal promotionalPercent, decimal expectedAmount)
    {
        var money = new Money(1000, Currency.RUB);

        var promotion = new Promotion(Guid.NewGuid(), promotionalPercent, DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

        var fakeStrategy = new IPricingStrategy[] {new VolumeStrategy()};
        var calculator = new PriceCalculator(fakeStrategy);

        Money promotionMoney = calculator.CalculatePromotion(money, promotion);

        Assert.Equal(expectedAmount, promotionMoney.Amount);
    }

    [Fact]
    public void IsActiveAt()
    {
        var promotion = new Promotion(Guid.NewGuid(), 25m, DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

        Assert.True(promotion.IsActiveAt(DateTime.UtcNow.AddHours(1)));
        Assert.False(promotion.IsActiveAt(DateTime.UtcNow.AddHours(3)));
    }
}
