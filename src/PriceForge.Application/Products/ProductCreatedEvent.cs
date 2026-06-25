namespace PriceForge.Application.Products;

public sealed record ProductCreatedEvent(Guid Id, string Name, string Sku, DateTime OccurredAt);
