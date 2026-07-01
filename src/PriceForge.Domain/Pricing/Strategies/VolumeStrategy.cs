using PriceForge.Domain.Common;

namespace PriceForge.Domain.Pricing.Strategies;

public sealed class VolumeStrategy : IPricingStrategy
{
    private const int VolumeThreshold = 100;
    private const decimal VolumeDiscount = 0.10m;

    public Money CalculatePrice(Money currentPrice, PricingContext context)
    {
        if (context.Quantity < VolumeThreshold)
        {
            return currentPrice;
        }

        decimal discounted = currentPrice.Amount * (1m - VolumeDiscount);

        return new Money(discounted, currentPrice.Currency);
    }
}
