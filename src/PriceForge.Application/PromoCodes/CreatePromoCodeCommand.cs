using MediatR;

namespace PriceForge.Application.PromoCodes;

public record CreatePromoCodeCommand
    (string Code, decimal DiscountPercent, DateTime ValidFrom, DateTime ValidTo, int UsedLimit, decimal MinOrderAmount)
    : IRequest<Guid>;
