using FluentValidation;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Products;

public sealed class GetProductPriceValidator : AbstractValidator<GetProductPriceQuery>
{
    public GetProductPriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Tier).IsInEnum();
        RuleFor(x => x.Region).IsInEnum();
    }
}
