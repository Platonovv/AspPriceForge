using FluentValidation;
using PriceForge.Domain.Common;

namespace PriceForge.Application.Products;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().Length(3, 64);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .IsEnumName(typeof(Currency))
            .WithMessage("Currency must be a valid code (USD, RUB, EUR...)");
    }
}
