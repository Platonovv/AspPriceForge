using PriceForge.Domain.Carts;

namespace PriceForge.UnitTests.CartTests;

[Trait("Category", "Unit")]
public class CartTest
{
    [Fact]
    public void AddCartItem()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cart = new Cart(userId);

        cart.AddCartItem(productId, 1);
        cart.AddCartItem(productId, 2);
        cart.AddCartItem(productId, 3);

        Assert.Equal(6, cart.FindCartItem(productId)!.Quantity);
        Assert.False(cart.IsEmpty());
    }

    [Fact]
    public void Clear()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cart = new Cart(userId);

        cart.AddCartItem(productId, 1);
        cart.AddCartItem(productId, 2);
        cart.AddCartItem(productId, 3);

        cart.Clear();

        Assert.True(cart.IsEmpty());
    }

    [Fact]
    public void RemoveCartItem()
    {
        var userId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();

        var cart = new Cart(userId);

        cart.AddCartItem(firstProductId, 1);
        cart.AddCartItem(secondProductId, 1);

        cart.RemoveComponent(firstProductId);

        Assert.False(cart.IsEmpty());
        Assert.Null(cart.FindCartItem(firstProductId));
        Assert.NotNull(cart.FindCartItem(secondProductId));
    }
}
