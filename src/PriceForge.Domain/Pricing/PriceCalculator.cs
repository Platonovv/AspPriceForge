using PriceForge.Domain.Categories;
using PriceForge.Domain.Common;
using PriceForge.Domain.PromoCodes;
using PriceForge.Domain.Promotions;

namespace PriceForge.Domain.Pricing;

public sealed class PriceCalculator
{
    private readonly IEnumerable<IPricingStrategy> _strategies;

    public PriceCalculator(IEnumerable<IPricingStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies;
    }

    public Money Calculate(PricingContext context)
    {
        Money calculatePrice = context.BasePrice;

        foreach (IPricingStrategy pricingStrategy in _strategies)
        {
            calculatePrice = pricingStrategy.CalculatePrice(calculatePrice, context);
        }

        return calculatePrice;
    }

    public Money CalculatePromotion(Money money,
                                    Promotion? promotion,
                                    IReadOnlyCollection<CategoryPromotion>? categories,
                                    DateTime at)
    {
        if (promotion != null && promotion.IsActiveAt(at))
        {
            decimal amount = CalculatePercent(money.Amount, promotion.Percent);
            return new Money(amount, money.Currency);
        }

        if (categories == null)
        {
            return money;
        }

        var activeCategories = categories.Where(c => c.IsActiveAt(at)).ToList();

        if (activeCategories.Count > 0)
        {
            decimal maxPromotionPercent = activeCategories.Max(c => c.Percent);
            decimal amount = CalculatePercent(money.Amount, maxPromotionPercent);
            return new Money(amount, money.Currency);
        }

        return money;
    }

    public decimal CalculatePromoCode(decimal money, PromoCode promoCode)
        => CalculatePercent(money, promoCode.DiscountPercent);

    private decimal CalculatePercent(decimal currentAmount, decimal discountPercent)
    {
        decimal amount = currentAmount * (1 - discountPercent / 100);
        return amount;
    }
}
