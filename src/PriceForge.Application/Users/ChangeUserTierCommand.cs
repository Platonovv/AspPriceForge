using MediatR;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Users;

public sealed record ChangeUserTierCommand(Guid UserId, CustomerTier Tier) : IRequest;
