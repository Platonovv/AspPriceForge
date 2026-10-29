using FluentValidation;
using PriceForge.Game.Domain.Purchases;

namespace PriceForge.Game.Application.Purchases;

public sealed class BuyGameItemValidator : AbstractValidator<BuyGameItemCommand>
{
    public BuyGameItemValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required");
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product Id is required");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(CoinPurchase.MaxIdempotencyKeyLength)
            .WithMessage($"Idempotency key must be 1-{CoinPurchase.MaxIdempotencyKeyLength} characters");
    }
}
