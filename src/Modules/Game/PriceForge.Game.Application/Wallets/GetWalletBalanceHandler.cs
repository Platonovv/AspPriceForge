using MediatR;
using Microsoft.Extensions.Logging;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Application.Wallets;

public sealed class GetWalletBalanceHandler : IRequestHandler<GetWalletBalanceQuery, long>
{
    private readonly TimeProvider _timeProvider;

    private readonly IWalletRepository _walletRepository;

    public GetWalletBalanceHandler(ILogger<GetWalletBalanceHandler> logger,
                                   IWalletRepository walletRepository,
                                   TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(walletRepository);

        _timeProvider = timeProvider;
        _walletRepository = walletRepository;
    }

    public async Task<long> Handle(GetWalletBalanceQuery query, CancellationToken cancellationToken)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return wallet?.Balance ?? 0L;
    }
}
