using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;

namespace PriceForge.Infrastructure.Persistence.Brokers;

internal class LoggingEventPublisher : IEventPublisher
{
    private readonly ILogger<LoggingEventPublisher> _logger;

    public LoggingEventPublisher(ILogger<LoggingEventPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        _logger.LogInformation("Publishing {Type}: {Content}", message.Type, message.Content);

        return Task.CompletedTask;
    }
}
