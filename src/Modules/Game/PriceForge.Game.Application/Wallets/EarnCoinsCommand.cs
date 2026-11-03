using MediatR;

namespace PriceForge.Game.Application.Wallets;

public sealed record EarnCoinsCommand(Guid UserId, long Coins) : IRequest<long>;
