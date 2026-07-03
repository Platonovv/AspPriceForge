using FluentValidation;

namespace PriceForge.Application.PromoCodes;

public sealed class CreatePromoCodeValidator : AbstractValidator<CreatePromoCodeCommand>
{
    public CreatePromoCodeValidator()
    {
        RuleFor(c => c.Code).NotEmpty().WithMessage("Promo Code field is required.");
        RuleFor(x => x.DiscountPercent)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Discount Percent must be greater than or equal to 100.");
        RuleFor(x => x.ValidTo).GreaterThan(x => x.ValidFrom).WithMessage("ValidTo < ValidFrom.");
        RuleFor(x => x.UsedLimit).GreaterThan(0).WithMessage("UsedLimit <= 0");
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).WithMessage("MinOrderAmount < 0");
    }
}
