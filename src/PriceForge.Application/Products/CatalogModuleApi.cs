using PriceForge.Catalog.Contracts;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

internal sealed class CatalogModuleApi : ICatalogModuleApi
{
    private readonly IProductRepository _productRepository;

    public CatalogModuleApi(IProductRepository productRepository)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        _productRepository = productRepository;
    }

    public async Task<CatalogProductDto?> GetProductAsync(Guid productId, CancellationToken ct)
    {
        Product? product = await _productRepository.GetByIdAsync(productId, ct);

        return product == null ? null : new CatalogProductDto(product.Id, product.Name);
    }

    public async Task<List<CatalogProductDto>> GetProductsAsync(List<Guid> productIds, CancellationToken ct)
    {
        List<Product> products = await _productRepository.GetByIdsAsync(productIds, ct);

        return products.Select(product => new CatalogProductDto(product.Id, product.Name)).ToList();
    }
}
