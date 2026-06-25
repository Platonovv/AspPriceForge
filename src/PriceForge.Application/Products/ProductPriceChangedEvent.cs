using PriceForge.Domain.Common;

namespace PriceForge.Application.Products;

public record ProductPriceChangedEvent
    (Guid Id, Guid ProductId, decimal NewPrice, Currency Currency, DateTime OccurredAt);
