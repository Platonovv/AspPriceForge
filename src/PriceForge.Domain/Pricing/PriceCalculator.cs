using PriceForge.Domain.Common;
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

    public Money CalculatePromotion(Money money, Promotion promotion)
    {
        decimal amount = money.Amount * (1 - promotion.Percent / 100);
        money = new Money(amount, money.Currency);
        return money;
    }


}
