namespace PriceForge.Application.Brokers;

public interface IEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct);
}
