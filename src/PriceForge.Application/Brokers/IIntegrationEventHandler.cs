namespace PriceForge.Application.Brokers;

public interface IIntegrationEventHandler
{
    string EventType { get; }
    Task HandleAsync(string message, CancellationToken ct);
}
