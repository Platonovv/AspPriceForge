using System.Text;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using RabbitMQ.Client;

namespace PriceForge.Infrastructure.Persistence.Brokers;

internal class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(ILogger<RabbitMqEventPublisher> logger, IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(connection);

        _logger = logger;
        _connection = connection;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        await using (IChannel channel = await _connection.CreateChannelAsync(cancellationToken: ct))
        {
            _logger.LogInformation("Publishing {@Event}", message);

            await channel.ExchangeDeclareAsync(RabbitMqTopology.EXCHANGE,
                                               ExchangeType.Topic,
                                               durable: true,
                                               cancellationToken: ct);

            byte[] messageInByte = Encoding.UTF8.GetBytes(message.Content);
            var basicProperties = new BasicProperties {Persistent = true};
            await channel.BasicPublishAsync(exchange: RabbitMqTopology.EXCHANGE,
                                            routingKey: message.Type,
                                            mandatory: false,
                                            basicProperties: basicProperties,
                                            body: messageInByte,
                                            cancellationToken: ct);
        }
    }
}
