using System.Text;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using RabbitMQ.Client;

namespace PriceForge.Infrastructure.Persistence.Brokers;

internal class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private IChannel? _channel;

    public RabbitMqEventPublisher(ILogger<RabbitMqEventPublisher> logger, IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(connection);

        _logger = logger;
        _connection = connection;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        if (_channel == null)
        {
            _channel = await _connection.CreateChannelAsync(
                           new CreateChannelOptions(publisherConfirmationsEnabled: true,
                                                    publisherConfirmationTrackingEnabled: true),
                           cancellationToken: ct);
            await RabbitMqTopology.DeclareExchangesAsync(_channel, ct);
        }

        _logger.LogInformation("Publishing {@Event}", message);

        byte[] messageInByte = Encoding.UTF8.GetBytes(message.Content);
        var basicProperties = new BasicProperties {Persistent = true};
        await _channel.BasicPublishAsync(exchange: RabbitMqTopology.EXCHANGE,
                                         routingKey: message.Type,
                                         mandatory: false,
                                         basicProperties: basicProperties,
                                         body: messageInByte,
                                         cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.DisposeAsync();
        }
    }
}
