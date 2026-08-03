namespace PriceForge.Infrastructure.Persistence.Records;

public sealed record CartRecord(Guid UserId, List<CartItemRecord> CartItemRecords);
