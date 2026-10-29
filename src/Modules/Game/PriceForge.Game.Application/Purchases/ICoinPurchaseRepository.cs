using PriceForge.Game.Domain.Purchases;

namespace PriceForge.Game.Application.Purchases;

public interface ICoinPurchaseRepository
{
    Task<CoinPurchase?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct);
    Task AddAsync(CoinPurchase purchase, CancellationToken ct);
}
