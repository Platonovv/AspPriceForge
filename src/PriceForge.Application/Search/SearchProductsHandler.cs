using MediatR;
using PriceForge.Application.Common;
using PriceForge.Application.Products;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Search;

public sealed class SearchProductsHandler : IRequestHandler<SearchProductsQuery, ProductSearchResultDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ProductPricingService _productPricingService;

    public SearchProductsHandler(IProductRepository productRepository, ProductPricingService productPricingService)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(productPricingService);

        _productRepository = productRepository;
        _productPricingService = productPricingService;
    }

    public async Task<ProductSearchResultDto> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        (IReadOnlyList<Product> Items, int Total) productsByFilter
            = await _productRepository.GetProductsByFilterAsync(request.SearchString,
                                                                request.ProductSearchFilter,
                                                                cancellationToken);

        var pricingItems = productsByFilter.Items
                                           .Select(product => new PricingItem(product.Id, product.BasePrice, 1))
                                           .ToList();

        IReadOnlyDictionary<Guid, Money> productPricing
            = await _productPricingService.CalculateProductPricingAsync(
                  pricingItems,
                  request.Tier,
                  request.Region,
                  cancellationToken);

        var searchProducts = new List<SearchProductDto>();

        foreach (Product product in productsByFilter.Items)
        {
            Money money = productPricing[product.Id];

            searchProducts.Add(new SearchProductDto(product.Name, product.Sku.Value, money.Amount));
        }

        return new ProductSearchResultDto(searchProducts, productsByFilter.Total);
    }
}
