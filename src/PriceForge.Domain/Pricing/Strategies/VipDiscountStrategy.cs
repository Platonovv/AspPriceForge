using PriceForge.Domain.Common;

namespace PriceForge.Domain.Pricing.Strategies;

public sealed class VipDiscountStrategy : IPricingStrategy
{
    public Money CalculatePrice(Money currentPrice, PricingContext context)
    {
        decimal discounted = currentPrice.Amount * TierPricing.GetMultiplier(context.Tier);

        return new Money(discounted, currentPrice.Currency);
    }
}
