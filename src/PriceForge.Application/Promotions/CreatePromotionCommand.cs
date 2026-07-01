using MediatR;

namespace PriceForge.Application.Promotions;

public record CreatePromotionCommand
    (Guid ProductId, decimal Percent, DateTime StartAt, DateTime EndAt) : IRequest<Guid>;
