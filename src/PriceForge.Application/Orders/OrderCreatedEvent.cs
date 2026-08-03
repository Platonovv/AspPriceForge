namespace PriceForge.Application.Orders;

public sealed record OrderCreatedEvent(Guid Id, DateTime OccurredAt);
