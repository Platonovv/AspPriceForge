using MediatR;

namespace PriceForge.Application.Wallets;

public sealed record EarnCoinsCommand(Guid UserId, long EarnMoney) : IRequest<long>;
