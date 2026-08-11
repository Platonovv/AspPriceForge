using PriceForge.Domain.Products;

namespace PriceForge.UnitTests.ProductTests;

[Trait("Category", "Unit")]
public class SkuTests
{
    [Theory]
    [InlineData("ABC")]
    [InlineData("SEARCH-1")]
    [InlineData("bulk_42")]
    public void TryCreate_AllowedFormat_ReturnsTrue(string value)
    {
        bool created = Sku.TryCreate(value, out Sku? sku);

        Assert.True(created);
        Assert.NotNull(sku);
        Assert.Equal(value, sku.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("Науш 1212")]
    [InlineData("ABC!")]
    public void TryCreate_NotAllowedFormat_ReturnsFalse(string? value)
    {
        bool created = Sku.TryCreate(value, out Sku? sku);

        Assert.False(created);
        Assert.Null(sku);
    }

    [Fact]
    public void TryCreate_TooLongValue_ReturnsFalse()
    {
        bool created = Sku.TryCreate(new string('a', 65), out Sku? sku);

        Assert.False(created);
        Assert.Null(sku);
    }

    [Fact]
    public void TryCreate_MaximumLength_ReturnsTrue()
    {
        bool created = Sku.TryCreate(new string('a', 64), out Sku? sku);

        Assert.True(created);
        Assert.NotNull(sku);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("ABC!")]
    public void Constructor_NotAllowedFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new Sku(value));
    }
}
