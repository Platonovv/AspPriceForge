using PriceForge.Domain.Pricing;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.UnitTests.CalculateTests;

[Trait("Category", "Unit")]
public class CalculatePromoCodeTest
{
    private static PromoCode Make(decimal percent, decimal minOrder, int usedLimit, DateTime from, DateTime to)
        => new(new Code("SALE"), percent, from, to, usedLimit, minOrder);

    [Theory]
    [InlineData(25, 750)]
    [InlineData(20, 800)]
    public void CalculatePromoCode_AppliesPercent(decimal percent, decimal expected)
    {
        var calculator = new PriceCalculator(Array.Empty<IPricingStrategy>());
        PromoCode code = Make(percent, 0m, 10, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        decimal result = calculator.CalculatePromoCode(1000m, code);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValid_WithinWindowUnderLimitAboveMin_True()
    {
        PromoCode code = Make(25m, 100m, 10, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        Assert.True(code.IsValid(1000m, DateTime.UtcNow).Valid);
    }

    [Fact]
    public void IsValid_Expired_False()
    {
        PromoCode code = Make(25m, 100m, 10, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));
        Assert.False(code.IsValid(1000m, DateTime.UtcNow).Valid);
    }

    [Fact]
    public void IsValid_BelowMinOrder_False()
    {
        PromoCode code = Make(25m, 500m, 10, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        Assert.False(code.IsValid(100m, DateTime.UtcNow).Valid);
    }

    [Fact]
    public void IsValid_UsageLimitReached_False()
    {
        PromoCode code = Make(25m, 0m, 1, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        code.IncrementUsage(); // UsedCount 0 -> 1, UsedLimit 1 => не < лимита
        Assert.False(code.IsValid(1000m, DateTime.UtcNow).Valid);
    }
}
