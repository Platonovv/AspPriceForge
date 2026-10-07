using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Promotions;
using PriceForge.Domain.Promotions;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfPromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _dbContext;

    public EfPromotionRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _dbContext = context;
    }

    public async Task AddAsync(Promotion promotion, CancellationToken ct)
        => await _dbContext.Promotions.AddAsync(promotion, ct);

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct)
        => _dbContext.Promotions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Promotion?> GetActiveForProductAsync(Guid productId, DateTime at, CancellationToken ct)
        => _dbContext.Promotions
                     .AsNoTracking()
                     .FirstOrDefaultAsync(x => x.ProductId == productId && x.StartAt <= at && at <= x.EndAt,
                                          cancellationToken: ct);

    public Task<List<Guid>> GetProductIdsWithBoundaryInWindowAsync(DateTime from, DateTime to, CancellationToken ct)
        => _dbContext.Promotions
                     .Where(p => (p.StartAt > from && p.StartAt <= to) || (p.EndAt > from && p.EndAt <= to))
                     .Select(x => x.ProductId)
                     .Distinct()
                     .ToListAsync(ct);

    public Task<List<Promotion>> GetActiveForProductsAsync(List<Guid> ids, DateTime at, CancellationToken ct)
        => _dbContext.Promotions
                     .Where(p => ids.Contains(p.ProductId) && p.StartAt <= at && at <= p.EndAt)
                     .ToListAsync(ct);

    public Task<bool> HasOverlappingAsync(Guid productId, DateTime startAt, DateTime endAt, CancellationToken ct)
        => _dbContext.Promotions.AnyAsync(x => x.ProductId == productId && x.StartAt < endAt && startAt < x.EndAt, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);
}
