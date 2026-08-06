using PriceForge.Domain.Pricing;

namespace PriceForge.Api.Contracts.Users;

public sealed record ChangeUserTierRequest(Guid UserId, CustomerTier Tier);
