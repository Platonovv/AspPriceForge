using MediatR;

namespace PriceForge.Game.Application.Wallets;

public sealed record EarnCoinsCommand(Guid UserId, long EarnMoney) : IRequest<long>;
