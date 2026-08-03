using PriceForge.Domain.Common;

namespace PriceForge.Application.Carts;

public record PricedCart(List<PricedCartItem> PricedCartItems, decimal TotalPrice, Currency Currency);
