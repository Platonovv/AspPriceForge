using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Application.Wallets;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<WalletTransaction?> GetByUserIdAndKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct);
    Task<Wallet?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Wallet wallet, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
