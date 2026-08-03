using PriceForge.Domain.Common;

namespace PriceForge.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public decimal ItemsTotal { get; private set; }
    public decimal Total { get; private set; }
    public string? PromoCodeName { get; private set; }
    public decimal TotalDiscountAmount { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public Currency Currency { get; private set; }
    public Status Status { get; private set; }

    private readonly List<OrderItem> _orderItems = new();

    public IReadOnlyList<OrderItem> OrderItems => _orderItems;

    public Order(List<OrderItem> orderItems,
                 Guid userId,
                 string? promoCodeName,
                 decimal discountPercent,
                 DateTime createdAt,
                 Currency currency,
                 Status status) : this(Guid.NewGuid(),
                                       orderItems,
                                       userId,
                                       promoCodeName,
                                       discountPercent,
                                       createdAt,
                                       currency,
                                       status)
    {
    }

    private Order()
    {
    }

    private Order(Guid id,
                  List<OrderItem> orderItems,
                  Guid userId,
                  string? promoCodeName,
                  decimal discountPercent,
                  DateTime createdAt,
                  Currency currency,
                  Status status)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty", nameof(userId));
        }

        if (createdAt == DateTime.MinValue)
        {
            throw new ArgumentException("Created at cannot be empty", nameof(createdAt));
        }

        if (orderItems.Count <= 0)
        {
            throw new ArgumentException("Order items cannot be empty", nameof(orderItems));
        }

        if (discountPercent is < 0 or > 100)
        {
            throw new ArgumentException("Discount percent must be between 0 and 100", nameof(discountPercent));
        }

        Id = id;
        _orderItems = orderItems.ToList();
        UserId = userId;
        PromoCodeName = promoCodeName;
        DiscountPercent = discountPercent;
        CreatedAt = createdAt;
        Currency = currency;
        Status = status;

        CalculateTotalPrice();
    }

    private void CalculateTotalPrice()
    {
        ItemsTotal = _orderItems.Sum(x => x.UnitPrice * x.Quantity);
        Total = ItemsTotal * (1 - DiscountPercent / 100);
        TotalDiscountAmount = ItemsTotal - Total;
    }
}
