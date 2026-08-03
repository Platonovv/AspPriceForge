namespace PriceForge.Domain.Orders;

public sealed record OrderItem
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = null!;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public OrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product Id is empty", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product Name is empty", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentException("Unit Price must be greater than zero", nameof(unitPrice));
        }

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
