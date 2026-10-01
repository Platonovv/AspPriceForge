using FluentValidation;
using PriceForge.Domain.Common;

namespace PriceForge.Application.Products;

public sealed class UpdateProductPriceValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .IsEnumName(typeof(Currency))
            .WithMessage("Currency must be a valid code (USD, RUB, EUR...)");
    }
}
