using MediatR;
using Microsoft.Extensions.Options;
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

        wallet.Earn(command.EarnMoney, _walletSettings.MaxCoinsPerSecond, _walletSettings.MaxEarnWindowSeconds, earnAt);

        await _walletRepository.SaveChangesAsync(cancellationToken);
        return wallet.Balance;
    }
}
