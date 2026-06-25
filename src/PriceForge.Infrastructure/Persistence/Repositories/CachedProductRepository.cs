using Microsoft.Extensions.Caching.Memory;
using PriceForge.Application.Products;
using PriceForge.Domain.Products;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IMemoryCache _cache;

    public CachedProductRepository(IProductRepository inner, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        string key = $"product:{id}";
        if (_cache.TryGetValue(key, out Product? cacheProduct))
        {
            return cacheProduct;
        }

        Product? productFromDb = await _inner.GetByIdAsync(id, ct);
        if (productFromDb is not null)
        {
            _cache.Set(key, productFromDb, TimeSpan.FromMinutes(5));
        }

        return productFromDb;
    }

    public Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        _cache.Remove($"product:{id}");
        return _inner.GetByIdForUpdateAsync(id, ct);
    }

    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken ct) => _inner.GetBySkuAsync(sku, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => _inner.ExistsAsync(id, ct);

    public Task AddAsync(Product product, CancellationToken ct) => _inner.AddAsync(product, ct);

    public void Remove(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        string key = $"product:{product.Id}";
        _cache.Remove(key);
        _inner.Remove(product);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => _inner.SaveChangesAsync(ct);
}
