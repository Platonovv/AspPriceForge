using FluentValidation;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Search;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    private const int MaxPageSize = 50;
    private const int MaxSearchStringLength = 200;

    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Region).IsInEnum().NotEqual(Region.None).WithMessage("Region must be a known value");

        RuleFor(x => x.SearchString)
            .MaximumLength(MaxSearchStringLength)
            .WithMessage($"Search string must not exceed {MaxSearchStringLength} characters");

        RuleFor(x => x.ProductSearchFilter).NotEmpty().WithMessage("Search filter must not be null");

        When(x => x.ProductSearchFilter is not null,
             () =>
             {
                 RuleFor(x => x.ProductSearchFilter.PageIndex)
                     .GreaterThan(0)
                     .WithMessage("Page index must be greater than zero");

                 RuleFor(x => x.ProductSearchFilter.PageSize)
                     .InclusiveBetween(1, MaxPageSize)
                     .WithMessage($"Page size must be between 1 and {MaxPageSize}");

                 RuleFor(x => x.ProductSearchFilter.MaxPrice)
                     .GreaterThan(0)
                     .When(x => x.ProductSearchFilter.MaxPrice.HasValue)
                     .WithMessage("Max price must be greater than zero");

                 RuleFor(x => x.ProductSearchFilter.Category)
                     .NotEqual(Guid.Empty)
                     .When(x => x.ProductSearchFilter.Category.HasValue)
                     .WithMessage("Category must not be empty");
             });
    }
}
