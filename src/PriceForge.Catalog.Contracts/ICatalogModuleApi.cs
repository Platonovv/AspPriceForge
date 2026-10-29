namespace PriceForge.Catalog.Contracts;

public interface ICatalogModuleApi
{
    Task<CatalogProductDto?> GetProductAsync(Guid productId, CancellationToken ct);
}
