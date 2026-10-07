namespace PriceForge.Domain.Pricing;

public static class TierPricing
{
    private const decimal VipDiscount = 0.15m;

    public static decimal GetMultiplier(CustomerTier tier)
    {
        switch (tier)
        {
            case CustomerTier.Vip:
                return 1 - VipDiscount;
            case CustomerTier.None:
            case CustomerTier.Regular:
            default:
                return 1m;
        }
    }
}
