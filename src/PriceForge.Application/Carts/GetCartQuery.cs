using MediatR;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Carts;

public sealed record GetCartQuery(Guid UserId, CustomerTier Tier, Region Region) : IRequest<CartTotalQueryDto>;
