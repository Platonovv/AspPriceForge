namespace PriceForge.Domain.Carts;

public sealed class Cart
{
    public Guid UserId { get; private set; }

    private readonly List<CartItem> _cartItems = new();

    public IReadOnlyList<CartItem> CartItems => _cartItems;

    private Cart()
    {
    }

    public Cart(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User Id cannot be empty", nameof(userId));
        }

        UserId = userId;
    }

    public void Clear() => _cartItems.Clear();

    public bool IsEmpty() => _cartItems.Count <= 0;

    public void AddCartItem(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity cannot be zero or negative", nameof(quantity));
        }

        CartItem? existing = FindCartItem(productId);

        if (existing != null)
        {
            ChangeComponentQuantity(productId, existing.Quantity + quantity);
        }
        else
        {
            _cartItems.Add(new CartItem(productId, quantity));
        }
    }

    public void RemoveComponent(Guid productId)
    {
        CartItem? cartItem = FindCartItem(productId);

        if (cartItem == null)
        {
            throw new KeyNotFoundException($"Component with {productId} not found in product {productId}");
        }

        _cartItems.Remove(cartItem);
    }

    public void ChangeComponentQuantity(Guid productId, int newQuantity)
    {
        CartItem? cartItem = FindCartItem(productId);

        if (cartItem == null)
        {
            throw new KeyNotFoundException($"Component with {productId} not found in product {productId}");
        }

        cartItem.ChangeQuantity(newQuantity);
    }

    public CartItem? FindCartItem(Guid itemId) => _cartItems.FirstOrDefault(c => c.ProductId == itemId);
}
