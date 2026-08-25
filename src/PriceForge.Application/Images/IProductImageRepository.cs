using PriceForge.Domain.Images;

namespace PriceForge.Application.Images;

public interface IProductImageRepository
{
    Task<ProductImage?> GetByProductIdAsync(Guid productId, CancellationToken ct);

    Task<List<string>> ListKeysAsync(CancellationToken stoppingToken);

    Task AddAsync(ProductImage productImage, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
