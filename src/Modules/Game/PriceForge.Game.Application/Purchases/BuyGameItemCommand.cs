using MediatR;

namespace PriceForge.Game.Application.Purchases;

public sealed record BuyGameItemCommand(Guid UserId, Guid ProductId, string IdempotencyKey)
    : IRequest<CoinPurchaseDto>;
