using MediatR;
using PriceForge.Application.Categories;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Promotions;
using PriceForge.Domain.Categories;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;

namespace PriceForge.Application.Products;

public sealed class GetProductPriceHandler : IRequestHandler<GetProductPriceQuery, MoneyDto>
{
    private readonly PriceCalculator _priceCalculator;
    private readonly IProductRepository _productRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly ICategoryPromotionRepository _categoryPromotionRepository;

    public GetProductPriceHandler(IProductRepository productRepository,
                                  IPromotionRepository promotionRepository,
                                  ICategoryPromotionRepository categoryPromotionRepository,
                                  PriceCalculator priceCalculator)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(priceCalculator);
        ArgumentNullException.ThrowIfNull(promotionRepository);
        ArgumentNullException.ThrowIfNull(categoryPromotionRepository);

        _priceCalculator = priceCalculator;
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
        _categoryPromotionRepository = categoryPromotionRepository;
    }

    public async Task<MoneyDto> Handle(GetProductPriceQuery request, CancellationToken cancellationToken)
    {
        Product? product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product not found");
        }

        var pricingContext = new PricingContext(product.BasePrice, request.Quantity, request.Tier, request.Region);

        Money money = _priceCalculator.Calculate(pricingContext);

        DateTime currentTime = DateTime.UtcNow;

        Promotion? promotion
            = await _promotionRepository.GetActiveForProductAsync(product.Id, currentTime, cancellationToken);

        IReadOnlyCollection<CategoryPromotion> categoryPromotions
            = await _categoryPromotionRepository.GetActiveCategoryPromotionsByProductAsync(
                  product.Id,
                  currentTime,
                  cancellationToken);

        money = _priceCalculator.CalculatePromotion(money, promotion, categoryPromotions, currentTime);

        return new MoneyDto(money.Amount, money.Currency);
    }
}
