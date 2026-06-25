using PriceForge.Domain.Common;

namespace PriceForge.Domain.Pricing;

public sealed record PricingContext
{
    public Money BasePrice { get; }
    public int Quantity { get; }
    public CustomerTier Tier { get; }
    public Region Region { get; }

    public PricingContext(Money basePrice, int quantity, CustomerTier tier, Region region)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive");
        }

        BasePrice = basePrice;
        Quantity = quantity;
        Tier = tier;
        Region = region;
    }
}
