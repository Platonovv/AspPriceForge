using FluentValidation;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.Application.Wallets;

public sealed class EarnCoinsValidator : AbstractValidator<EarnCoinsCommand>
{
    public EarnCoinsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(WalletTransaction.MaxIdempotencyKeyLength)
            .WithMessage($"Idempotency key must be 1-{WalletTransaction.MaxIdempotencyKeyLength} characters");
    }
}
