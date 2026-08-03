namespace PriceForge.Domain.Carts;

public sealed class CartItem
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private CartItem()
    {
    }

    internal CartItem(Guid productId, int quantity)
    {
        ValidateQuantity(quantity);

        ProductId = productId;
        Quantity = quantity;
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
        }
    }

    internal void ChangeQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);

        Quantity = newQuantity;
    }
}
