using MediatR;
using PriceForge.Application.Products;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Search;

public sealed class SearchProductsHandler : IRequestHandler<SearchProductsQuery, ProductSearchResultDto>
{
    private readonly IProductRepository _productRepository;

    public SearchProductsHandler(IProductRepository productRepository)
    {
        ArgumentNullException.ThrowIfNull(productRepository);

        _productRepository = productRepository;
    }

    public async Task<ProductSearchResultDto> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        ProductSearchFilter productSearchFilter = request.ProductSearchFilter with
        {
            MaxPrice = request.ProductSearchFilter.MaxPrice / TierPricing.GetMultiplier(request.Tier)
        };

        (IReadOnlyList<Product> Items, int Total) productsByFilter
            = await _productRepository.GetProductsByFilterAsync(request.SearchString,
                                                                productSearchFilter,
                                                                cancellationToken);

        var searchProducts = productsByFilter.Items
                                             .Select(product =>
                                             {
                                                 decimal price = PriceRounding.RoundToCents(product.PublicPrice
                                                     * TierPricing.GetMultiplier(request.Tier));
                                                 return new SearchProductDto(product.Name, product.Sku.Value, price);
                                             })
                                             .ToList();

        return new ProductSearchResultDto(searchProducts, productsByFilter.Total);
    }
}
