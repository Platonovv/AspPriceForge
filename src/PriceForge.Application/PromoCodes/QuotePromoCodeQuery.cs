using MediatR;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.PromoCodes;

public sealed record QuotePromoCodeQuery
    (Guid UserId, string Code, CustomerTier Tier, Region Region) : IRequest<QuotePromoCodeDto>;
