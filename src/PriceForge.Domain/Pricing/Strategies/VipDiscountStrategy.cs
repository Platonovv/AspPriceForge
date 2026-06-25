using PriceForge.Domain.Common;

namespace PriceForge.Domain.Pricing.Strategies;

public sealed class VipDiscountStrategy : IPricingStrategy
{
    private const decimal VipDiscount = 0.15m;

    public Money CalculatePrice(Money currentPrice, PricingContext context)
    {
        if (context.Tier != CustomerTier.Vip)
        {
            return currentPrice;
        }

        decimal discounted = currentPrice.Amount * (1m - VipDiscount);

        return new Money(discounted, currentPrice.Currency);
    }
}
