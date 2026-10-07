using PriceForge.Domain.Promotions;

namespace PriceForge.Application.Promotions;

public interface IPromotionRepository
{
    Task AddAsync(Promotion promotion, CancellationToken ct);
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Promotion?> GetActiveForProductAsync(Guid productId, DateTime at, CancellationToken ct);
    Task<List<Guid>> GetProductIdsWithBoundaryInWindowAsync(DateTime from, DateTime to, CancellationToken ct);
    Task<List<Promotion>> GetActiveForProductsAsync(List<Guid> ids, DateTime at, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<bool> HasOverlappingAsync(Guid productId, DateTime startAt, DateTime endAt, CancellationToken ct);
}
