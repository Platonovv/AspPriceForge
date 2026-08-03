using PriceForge.Domain.Carts;
using PriceForge.Infrastructure.Persistence.Records;

namespace PriceForge.Infrastructure.Persistence.Mappers;

public static class CartMapper
{
    public static CartRecord MapCartToRecord(Cart cart)
        => new CartRecord(cart.UserId,
                          cart.CartItems.Select(x => new CartItemRecord(x.ProductId, x.Quantity)).ToList());

    public static Cart MapCartRecordToCart(CartRecord cartRecord)
    {
        var cart = new Cart(cartRecord.UserId);
        foreach (CartItemRecord cartRecordCartItemRecord in cartRecord.CartItemRecords)
        {
            cart.AddCartItem(cartRecordCartItemRecord.ProductId, cartRecordCartItemRecord.Quantity);
        }

        return cart;
    }
}
