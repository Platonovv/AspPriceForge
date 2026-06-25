using PriceForge.Domain.Common;

namespace PriceForge.Domain.Pricing.Specifications;

public sealed class VipCustomerSpecification : ISpecification<PricingContext>
{
    public bool IsSatisfiedBy(PricingContext candidate) => candidate.Tier == CustomerTier.Vip;
}
