using PriceForge.Application.Carts;
using PriceForge.Application.Categories;
using PriceForge.Application.Promotions;
using PriceForge.Domain.Categories;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Promotions;

namespace PriceForge.Application.Common;

public sealed class ProductPricingService
{
    private readonly PriceCalculator _priceCalculator;

    private readonly IPromotionRepository _promotionRepository;

    private readonly ICategoryPromotionRepository _categoryPromotionRepository;

    public ProductPricingService(IPromotionRepository promotionRepository,
                                 ICategoryPromotionRepository categoryPromotionRepository,
                                 PriceCalculator priceCalculator)
    {
        ArgumentNullException.ThrowIfNull(priceCalculator);
        ArgumentNullException.ThrowIfNull(promotionRepository);
        ArgumentNullException.ThrowIfNull(categoryPromotionRepository);

        _priceCalculator = priceCalculator;
        _promotionRepository = promotionRepository;
        _categoryPromotionRepository = categoryPromotionRepository;
    }

    public async Task<IReadOnlyDictionary<Guid, Money>> CalculateProductPricingAsync(
        IReadOnlyCollection<PricingItem> pricingItems,
        CustomerTier tier,
        Region region,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, Money> resultDictionary = new();

        if (pricingItems.Count == 0)
        {
            return resultDictionary;
        }

        var productIds = pricingItems.Select(x => x.ProductId).ToList();

        DateTime currentTime = DateTime.UtcNow;

        List<Promotion> productPromotions
            = await _promotionRepository.GetActiveForProductsAsync(productIds, currentTime, cancellationToken);

        List<ProductCategoryPromotion> productCategoryPromotions
            = await _categoryPromotionRepository.GetActiveForProductsAsync(productIds, currentTime, cancellationToken);

        var productPromotionsDictionary = productPromotions.ToDictionary(p => p.ProductId);
        var productCategoryPromotionsDictionary = productCategoryPromotions.GroupBy(x => x.ProductId)
                                                                           .ToDictionary(
                                                                               g => g.Key,
                                                                               g => g.Select(x => x.Promotion)
                                                                                   .ToList());

        foreach (PricingItem item in pricingItems)
        {
            var pricingContext = new PricingContext(item.BasePrice, item.Quantity, tier, region);

            Money money = _priceCalculator.Calculate(pricingContext);

            productPromotionsDictionary.TryGetValue(item.ProductId, out Promotion? promotion);
            productCategoryPromotionsDictionary.TryGetValue(item.ProductId,
                                                            out List<CategoryPromotion>? categoryPromotions);

            money = _priceCalculator.CalculatePromotion(money, promotion, categoryPromotions, currentTime);

            resultDictionary[item.ProductId] = money;
        }

        return resultDictionary;
    }
}
