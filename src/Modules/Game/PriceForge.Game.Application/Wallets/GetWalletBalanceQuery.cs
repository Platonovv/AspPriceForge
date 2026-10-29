using MediatR;

namespace PriceForge.Game.Application.Wallets;

public sealed record GetWalletBalanceQuery(Guid UserId) : IRequest<long>;
