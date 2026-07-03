using FluentValidation;
using MediatR;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Application.PromoCodes;

public sealed class ApplyPromoCodeHandler : IRequestHandler<ApplyPromoCodeCommand, decimal>
{
    private readonly IPromoCodeRepository _repository;
    private readonly PriceCalculator _priceCalculator;

    public ApplyPromoCodeHandler(IPromoCodeRepository repository, PriceCalculator priceCalculator)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(priceCalculator);
        _priceCalculator = priceCalculator;
        _repository = repository;
    }

    public async Task<decimal> Handle(ApplyPromoCodeCommand request, CancellationToken cancellationToken)
    {
        PromoCode? promoCode = await _repository.GetByCodeAsync(request.Code, cancellationToken);

        if (promoCode == null)
        {
            throw new NotFoundException("PromoCode not found");
        }

        if (!promoCode.IsValid(request.OrderMoney, DateTime.UtcNow))
        {
            throw new ValidationException("PromoCode is invalid");
        }

        if (!await _repository.TryIncrementUsageAsync(promoCode, cancellationToken))
        {
            throw new ConflictException("PromoCode usage limit reached");
        }

        return _priceCalculator.CalculatePromoCode(request.OrderMoney, promoCode);
    }
}
