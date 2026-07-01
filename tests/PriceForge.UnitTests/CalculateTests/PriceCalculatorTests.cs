using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Pricing.Strategies;

namespace PriceForge.UnitTests.CalculateTests;

[Trait("Category", "Unit")]
public class PriceCalculatorTests
{
    public static IEnumerable<object[]> PricingCases()
    {
        yield return new object[] {100, CustomerTier.Vip, 765m, Region.RU};  // объём+VIP
        yield return new object[] {100, CustomerTier.None, 900m, Region.RU}; // только объём
        yield return new object[] {50, CustomerTier.Vip, 850m, Region.RU};   // только VIP
    }

    [Theory]
    [MemberData(nameof(PricingCases))]
    public void Calculate_AppliesStrategiesInChain(int quantity, CustomerTier tier, decimal expected, Region region)
    {
        var strategies = new IPricingStrategy[] {new VolumeStrategy(), new VipDiscountStrategy()};
        var calculator = new PriceCalculator(strategies);
        var money = new Money(1000m, Currency.RUB);
        var pricingContext = new PricingContext(money, quantity, tier, region);

        Money result = calculator.Calculate(pricingContext);

        Assert.Equal(expected, result.Amount);
    }

    [Theory]
    [InlineData(50, 1000)]
    [InlineData(99, 1000)]
    [InlineData(100, 900)]
    [InlineData(200, 900)]
    public void CalculatePrice_QuantityAtThreshold_Applies10PercentDiscount(int quantity, decimal expectedAmount)
    {
        var strategy = new VolumeStrategy();
        var money = new Money(1000, Currency.RUB);
        var pricingContext = new PricingContext(money, quantity, CustomerTier.Vip, Region.RU);

        Money result = strategy.CalculatePrice(money, pricingContext);

        Assert.Equal(expectedAmount, result.Amount);
    }
}
