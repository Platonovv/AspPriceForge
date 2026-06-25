using PriceForge.Domain.Common;

namespace PriceForge.Domain.Pricing;

public interface IPricingStrategy
{
    Money CalculatePrice(Money currentPrice, PricingContext context);
}
