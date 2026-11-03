using MediatR;

namespace PriceForge.Game.Application.Wallets;

public sealed record EarnCoinsCommand(Guid UserId, string IdempotencyKey, long Coins) : IRequest<long>;
