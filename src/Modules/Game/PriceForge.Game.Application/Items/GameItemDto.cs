namespace PriceForge.Game.Application.Items;

public sealed record GameItemDto(Guid ProductId, string Name, long CoinPrice);
