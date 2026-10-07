using PriceForge.Application.Common;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public sealed class PublicPriceRecalculator
{
    private readonly IProductRepository _productRepository;
    private readonly ProductPricingService _productPricingService;

    private const CustomerTier PublicTier = CustomerTier.Regular;
    private const Region PublicRegion = Region.None;

    public PublicPriceRecalculator(IProductRepository productRepository, ProductPricingService productPricingService)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(productPricingService);

        _productRepository = productRepository;
        _productPricingService = productPricingService;
    }

    public async Task RecalculateAsync(List<Guid> productIds, CancellationToken ct)
    {
        List<Product> products = await _productRepository.GetByIdsForUpdateAsync(productIds, ct);

        if (products.Count == 0)
        {
            return;
        }

        var pricingItems = products.Select(x => new PricingItem(x.Id, x.BasePrice, 1)).ToList();

        IReadOnlyDictionary<Guid, Money> result
            = await _productPricingService.CalculateProductPricingAsync(pricingItems, PublicTier, PublicRegion, ct);

        foreach (Product product in products)
        {
            product.UpdatePublicPrice(result[product.Id].Amount);
        }
    }
}
