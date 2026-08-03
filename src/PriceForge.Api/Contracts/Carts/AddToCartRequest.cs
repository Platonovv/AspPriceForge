namespace PriceForge.Api.Contracts.Carts;

public sealed record AddToCartRequest(Guid ProductId, int Quantity);
