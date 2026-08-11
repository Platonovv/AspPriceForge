using FluentValidation.Results;
using PriceForge.Application.Search;
using PriceForge.Domain.Pricing;

namespace PriceForge.UnitTests.SearchTests;

[Trait("Category", "Unit")]
public class SearchProductsQueryValidatorTests
{
    private readonly SearchProductsQueryValidator _validator = new();

    private static SearchProductsQuery CreateQuery(ProductSearchFilter filter, string? searchString = "наушники")
        => new(searchString, filter, CustomerTier.Regular, Region.RU);

    private static ProductSearchFilter CreateFilter(Guid? category = null,
                                                    decimal? maxPrice = null,
                                                    int pageIndex = 1,
                                                    int pageSize = 20)
        => new(category, maxPrice, pageIndex, pageSize);

    [Fact]
    public void Validate_DefaultFilter_IsValid()
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutSearchString_IsValid()
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(), null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositivePageIndex_IsInvalid(int pageIndex)
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(pageIndex: pageIndex)));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_PageSizeOutOfRange_IsInvalid(int pageSize)
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(pageSize: pageSize)));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Validate_PageSizeOnBoundary_IsValid(int pageSize)
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(pageSize: pageSize)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroMaxPrice_IsInvalid()
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(maxPrice: 0m)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCategory_IsInvalid()
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(Guid.Empty)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_UnknownRegion_IsInvalid()
    {
        var query = new SearchProductsQuery("наушники", CreateFilter(), CustomerTier.Regular, (Region)99);

        ValidationResult result = _validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RegionNone_IsInvalid()
    {
        var query = new SearchProductsQuery("наушники", CreateFilter(), CustomerTier.Regular, Region.None);

        ValidationResult result = _validator.Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TooLongSearchString_IsInvalid()
    {
        ValidationResult result = _validator.Validate(CreateQuery(CreateFilter(), new string('a', 201)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullFilter_IsInvalidWithoutThrowing()
    {
        var query = new SearchProductsQuery("наушники", null!, CustomerTier.Regular, Region.RU);

        ValidationResult result = _validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
