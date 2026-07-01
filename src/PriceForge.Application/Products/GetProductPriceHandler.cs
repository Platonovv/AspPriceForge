using MediatR;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Promotions;
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

    public GetProductPriceHandler(IProductRepository productRepository,
                                  IPromotionRepository promotionRepository,
                                  PriceCalculator priceCalculator)
    {
        _promotionRepository = promotionRepository;
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(priceCalculator);

        _priceCalculator = priceCalculator;
        _productRepository = productRepository;
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

        Promotion? promotion
            = await _promotionRepository.GetActiveForProductAsync(product.Id, DateTime.UtcNow, cancellationToken);

        if (promotion != null)
        {
            money = _priceCalculator.CalculatePromotion(money, promotion);
        }

        return new MoneyDto(money.Amount, money.Currency);
    }
}
