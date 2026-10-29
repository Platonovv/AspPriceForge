namespace PriceForge.Game.Application.Purchases;

public sealed record CoinPurchaseDto(Guid PurchaseId, Guid ProductId, long CoinPrice, DateTime CreatedAt);
