using PriceForge.Domain.Common;

namespace PriceForge.Application.Carts;

public sealed record PricedCartItem
    (Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal ItemsTotal, Currency Currency);
