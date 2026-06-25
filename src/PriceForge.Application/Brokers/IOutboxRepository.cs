namespace PriceForge.Application.Brokers;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
    Task<List<OutboxMessage>> GetUnpublishedAsync(CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
