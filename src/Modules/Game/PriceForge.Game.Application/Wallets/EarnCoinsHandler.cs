using MediatR;
using Microsoft.Extensions.Options;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Application.Wallets;

public sealed class EarnCoinsHandler : IRequestHandler<EarnCoinsCommand, long>
{
    private readonly TimeProvider _timeProvider;
    private readonly WalletSettings _walletSettings;
    private readonly IWalletRepository _walletRepository;

    public EarnCoinsHandler(IOptions<WalletSettings> walletSettings,
                            IWalletRepository walletRepository,
                            TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(walletSettings);
        ArgumentNullException.ThrowIfNull(walletRepository);

        _timeProvider = timeProvider;
        _walletRepository = walletRepository;
        _walletSettings = walletSettings.Value;
    }

    public async Task<long> Handle(EarnCoinsCommand command, CancellationToken cancellationToken)
    {
        DateTime earnAt = _timeProvider.GetUtcNow().UtcDateTime;

        Wallet? wallet = await _walletRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (wallet == null)
        {
            wallet = new Wallet(command.UserId, earnAt);

            await _walletRepository.AddAsync(wallet, cancellationToken);
        }
        else
        {
            WalletTransaction? walletTransaction
                = await _walletRepository.GetByUserIdAndKeyAsync(wallet.UserId,
                                                                 command.IdempotencyKey,
                                                                 cancellationToken);

            if (walletTransaction != null)
            {
                return walletTransaction.Amount != command.Coins
                           ? throw new IdempotencyKeyReuseException("IdempotencyKey used in another sum")
                           : wallet.Balance;
            }
        }

        wallet.Earn(command.Coins,
                    _walletSettings.MaxCoinsPerSecond,
                    _walletSettings.MaxEarnWindowSeconds,
                    command.IdempotencyKey,
                    earnAt);

        await _walletRepository.SaveChangesAsync(cancellationToken);

        return wallet.Balance;
    }
}
