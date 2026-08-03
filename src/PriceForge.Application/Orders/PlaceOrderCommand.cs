using MediatR;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Orders;

public record PlaceOrderCommand(Guid UserId, string? PromoCode, CustomerTier Tier, Region Region) : IRequest<Guid>;
