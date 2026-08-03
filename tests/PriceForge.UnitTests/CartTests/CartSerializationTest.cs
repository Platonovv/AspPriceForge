using System.Text.Json;
using PriceForge.Domain.Carts;
using PriceForge.Infrastructure.Persistence.Mappers;
using PriceForge.Infrastructure.Persistence.Records;

namespace PriceForge.UnitTests.CartTests;

[Trait("Category", "Unit")]
public class CartSerializationTest
{
    [Fact]
    public void RoundTrip_KeepsItems()
    {
        var userId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();

        var cart = new Cart(userId);
        cart.AddCartItem(firstProductId, 2);
        cart.AddCartItem(secondProductId, 3);

        CartRecord cartRecord = CartMapper.MapCartToRecord(cart);
        string json = JsonSerializer.Serialize(cartRecord);

        CartRecord restoredRecord = JsonSerializer.Deserialize<CartRecord>(json)!;
        Cart restored = CartMapper.MapCartRecordToCart(restoredRecord);

        Assert.Equal(userId, restored.UserId);
        Assert.Equal(2, restored.CartItems.Count);
        Assert.Equal(2, restored.FindCartItem(firstProductId)?.Quantity);
        Assert.Equal(3, restored.FindCartItem(secondProductId)?.Quantity);
    }
}
