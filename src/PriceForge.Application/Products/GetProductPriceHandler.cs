using MediatR;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public sealed class GetProductPriceHandler : IRequestHandler<GetProductPriceQuery, MoneyDto>
{
    private readonly PriceCalculator _priceCalculator;
    private readonly IProductRepository _productRepository;

    public GetProductPriceHandler(IProductRepository productRepository, PriceCalculator priceCalculator)
    {
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
            throw new InvalidOperationException("Product not found");
        }

        var pricingContext = new PricingContext(product.BasePrice, request.Quantity, request.Tier, request.Region);

        Money money = _priceCalculator.Calculate(pricingContext);

        return new MoneyDto(money.Amount, money.Currency);
    }
}
