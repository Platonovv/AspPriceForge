using MediatR;

namespace PriceForge.Application.Wallets;

public sealed record GetWalletBalanceQuery(Guid UserId) : IRequest<long>;
