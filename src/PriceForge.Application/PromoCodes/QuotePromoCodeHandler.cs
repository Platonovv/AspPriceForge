using MediatR;
using PriceForge.Application.Carts;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Application.PromoCodes;

public sealed class QuotePromoCodeHandler : IRequestHandler<QuotePromoCodeQuery, QuotePromoCodeDto>
{
    private readonly IPromoCodeRepository _repository;
    private readonly PriceCalculator _priceCalculator;
    private readonly CartPricingService _cartPricingService;

    public QuotePromoCodeHandler(IPromoCodeRepository repository,
                                 PriceCalculator priceCalculator,
                                 CartPricingService cartPricingService)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(priceCalculator);
        ArgumentNullException.ThrowIfNull(cartPricingService);

        _repository = repository;
        _priceCalculator = priceCalculator;
        _cartPricingService = cartPricingService;
    }

    public async Task<QuotePromoCodeDto> Handle(QuotePromoCodeQuery request, CancellationToken cancellationToken)
    {
        PricedCart priceCart = await _cartPricingService.CalculatePrice(request.UserId,
                                                                        request.Tier,
                                                                        request.Region,
                                                                        cancellationToken);

        var code = new Code(request.Code);
        PromoCode? promoCode = await _repository.GetByCodeAsync(code.Value, cancellationToken);

        if (promoCode == null)
        {
            return new QuotePromoCodeDto(priceCart.TotalPrice, priceCart.Currency, PromoCodeReason.IsPromoCodeNotFound);
        }

        (bool Valid, PromoCodeReason Reason) promoCodeTuple = promoCode.IsValid(priceCart.TotalPrice, DateTime.UtcNow);

        if (!promoCodeTuple.Valid)
        {
            return new QuotePromoCodeDto(priceCart.TotalPrice, priceCart.Currency, promoCodeTuple.Reason);
        }

        return new QuotePromoCodeDto(_priceCalculator.CalculatePromoCode(priceCart.TotalPrice, promoCode),
                                     priceCart.Currency,
                                     promoCodeTuple.Reason);
    }
}
