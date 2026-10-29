using Microsoft.EntityFrameworkCore;
using PriceForge.Game.Application.Purchases;
using PriceForge.Game.Domain.Purchases;

namespace PriceForge.Game.Infrastructure.Persistence.Repositories;

internal sealed class EfCoinPurchaseRepository : ICoinPurchaseRepository
{
    private readonly GameDbContext _dbContext;

    public EfCoinPurchaseRepository(GameDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<CoinPurchase?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct)
        => _dbContext.CoinPurchases.AsNoTracking()
                     .FirstOrDefaultAsync(p => p.UserId == userId && p.IdempotencyKey == idempotencyKey, ct);

    public async Task AddAsync(CoinPurchase purchase, CancellationToken ct)
        => await _dbContext.CoinPurchases.AddAsync(purchase, ct);
}
