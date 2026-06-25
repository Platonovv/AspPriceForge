using PriceForge.Domain.Common;

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
}
