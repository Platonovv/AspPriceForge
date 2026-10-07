using PriceForge.Domain.Common;
using PriceForge.Domain.Products;

namespace PriceForge.UnitTests.ProductTests;

[Trait("Category", "Unit")]
public class ProductPublicPriceTests
{
    private static Product CreateProduct(decimal basePrice)
        => new(new Sku("PUB-1"), "Public price product", new Money(basePrice, Currency.RUB));

    [Fact]
    public void NewProduct_PublicPriceEqualsBasePrice()
    {
        Product product = CreateProduct(5500m);

        Assert.Equal(5500m, product.PublicPrice);
    }

    [Theory]
    [InlineData(4675)]
    [InlineData(5500)]
    [InlineData(0)]
    public void UpdatePublicPrice_WithinZeroAndBase_IsApplied(decimal amount)
    {
        Product product = CreateProduct(5500m);

        product.UpdatePublicPrice(amount);

        Assert.Equal(amount, product.PublicPrice);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5500.01)]
    public void UpdatePublicPrice_OutsideZeroAndBase_Throws(decimal amount)
    {
        Product product = CreateProduct(5500m);

        Assert.Throws<ArgumentException>(() => product.UpdatePublicPrice(amount));
        Assert.Equal(5500m, product.PublicPrice);
    }

    [Theory]
    [InlineData(6000)]
    [InlineData(4000)]
    public void ChangeBasePrice_ResetsPublicPriceToNewBase(decimal newBase)
    {
        Product product = CreateProduct(5500m);
        product.UpdatePublicPrice(4675m);

        product.ChangeBasePrice(new Money(newBase, Currency.RUB));

        Assert.Equal(newBase, product.PublicPrice);
    }
}
