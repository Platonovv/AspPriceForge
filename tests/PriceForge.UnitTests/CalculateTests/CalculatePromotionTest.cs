using PriceForge.Domain.Categories;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Promotions;

namespace PriceForge.UnitTests.CalculateTests;

[Trait("Category", "Unit")]
public class CalculatePromotionTest
{
    private static readonly DateTime Now = new(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Money Price = new(1000m, Currency.RUB);

    private static PriceCalculator Calculator() => new(Array.Empty<IPricingStrategy>());

    private static Promotion ProductPromo(decimal percent, bool active = true)
        => active
               ? new Promotion(Guid.NewGuid(), percent, Now.AddHours(-1), Now.AddHours(1))
               : new Promotion(Guid.NewGuid(), percent, Now.AddHours(-5), Now.AddHours(-3));

    private static CategoryPromotion CategoryPromo(decimal percent, bool active = true)
        => active
               ? new CategoryPromotion(Guid.NewGuid(), percent, Now.AddHours(-1), Now.AddHours(1))
               : new CategoryPromotion(Guid.NewGuid(), percent, Now.AddHours(-5), Now.AddHours(-3));

    [Fact]
    public void NoPromotions_PriceUnchanged()
    {
        Money result = Calculator().CalculatePromotion(Price, null, Array.Empty<CategoryPromotion>(), Now);

        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public void OnlyProductPromotion_AppliesIt()
    {
        Money result = Calculator().CalculatePromotion(Price, ProductPromo(10m), Array.Empty<CategoryPromotion>(), Now);

        Assert.Equal(900m, result.Amount);
    }

    [Fact]
    public void OnlyCategoryPromotions_AppliesTheBiggest()
    {
        CategoryPromotion[] categories = [CategoryPromo(10m), CategoryPromo(30m)];

        Money result = Calculator().CalculatePromotion(Price, null, categories, Now);

        Assert.Equal(700m, result.Amount);
    }

    [Fact]
    public void ProductPromotionWins_EvenWhenCategoryDiscountIsBigger()
    {
        CategoryPromotion[] categories = [CategoryPromo(30m)];

        Money result = Calculator().CalculatePromotion(Price, ProductPromo(5m), categories, Now);

        Assert.Equal(950m, result.Amount);
    }

    [Fact]
    public void ExpiredProductPromotion_FallsBackToCategory()
    {
        CategoryPromotion[] categories = [CategoryPromo(30m)];

        Money result = Calculator().CalculatePromotion(Price, ProductPromo(5m, false), categories, Now);

        Assert.Equal(700m, result.Amount);
    }

    [Fact]
    public void ExpiredCategoryPromotion_IsIgnored()
    {
        CategoryPromotion[] categories = [CategoryPromo(30m, false), CategoryPromo(10m)];

        Money result = Calculator().CalculatePromotion(Price, null, categories, Now);

        Assert.Equal(900m, result.Amount);
    }

    [Fact]
    public void AllExpired_PriceUnchanged()
    {
        CategoryPromotion[] categories = [CategoryPromo(30m, false)];

        Money result = Calculator().CalculatePromotion(Price, ProductPromo(5m, false), categories, Now);

        Assert.Equal(1000m, result.Amount);
    }

    [Theory]
    [InlineData(25, 750)]
    [InlineData(20, 800)]
    [InlineData(90, 100)]
    public void ProductPromotion_AppliesPercent(decimal percent, decimal expected)
    {
        Money result = Calculator()
            .CalculatePromotion(Price, ProductPromo(percent), Array.Empty<CategoryPromotion>(), Now);

        Assert.Equal(expected, result.Amount);
    }

    [Fact]
    public void IsActiveAt_RespectsWindow()
    {
        Promotion promotion = ProductPromo(25m);

        Assert.True(promotion.IsActiveAt(Now));
        Assert.False(promotion.IsActiveAt(Now.AddHours(5)));
    }
}
