using PriceForge.Application.Search;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public interface IProductRepository
{
    // Чтение
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
    Task<Product?> GetBySkuAsync(Sku sku, CancellationToken ct);
    Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct);
    Task<List<Product>> GetByIdsForUpdateAsync(List<Guid> ids, CancellationToken ct);

    Task<(IReadOnlyList<Product> Items, int Total)> GetProductsByFilterAsync(
        string? searchString,
        ProductSearchFilter filter,
        CancellationToken ct);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    // Запись
    Task AddAsync(Product product, CancellationToken ct);
    void Remove(Product product);
    void ClearTracking();

    // Сохранение всех накопленных изменений (UoW commit)
    Task<int> SaveChangesAsync(CancellationToken ct);
}
