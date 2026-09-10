using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PriceForge.Infrastructure.Persistence.Brokers;

public sealed class RabbitMqConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private const int MaxRetries = 5;
    private const string RetryCountHeader = "x-retry-count";

    public RabbitMqConsumer(IConnection connection,
                            ILogger<RabbitMqConsumer> logger,
                            IServiceScopeFactory serviceScopeFactory)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);

        _logger = logger;
        _connection = connection;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMqConsumer started");

        IChannel productChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        IChannel emailChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(productChannel, stoppingToken);

        await BasicConsumeAsync(RabbitMqTopology.PRODUCT_QUEUE, productChannel, stoppingToken);
        await BasicConsumeAsync(RabbitMqTopology.EMAIL_QUEUE, emailChannel, stoppingToken);
    }

    private async Task BasicConsumeAsync(string queueName, IChannel channel, CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, eventArgs) => OnMessageAsync(channel, eventArgs, stoppingToken);

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken: stoppingToken);
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken ct)
    {
        string message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        string routingKey = eventArgs.RoutingKey;

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IIntegrationEventHandler? handler = scope.ServiceProvider
                                                 .GetServices<IIntegrationEventHandler>()
                                                 .FirstOrDefault(h => h.EventType == routingKey);

        if (handler is null)
        {
            _logger.LogWarning("No handler for event {RoutingKey}, acking", routingKey);
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, ct);
            return;
        }

        try
        {
            await handler.HandleAsync(message, ct);
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, ct);
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(channel, eventArgs, message, routingKey, ex, ct);
        }
    }

    private async Task HandleFailureAsync(IChannel channel,
                                          BasicDeliverEventArgs eventArgs,
                                          string message,
                                          string routingKey,
                                          Exception ex,
                                          CancellationToken ct)
    {
        int retryCount = GetRetryCount(eventArgs);

        if (retryCount >= MaxRetries)
        {
            _logger.LogError(ex, "Event {RoutingKey} failed {Count} times, dead-lettering", routingKey, retryCount);
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, ct);
            return;
        }

        _logger.LogWarning(ex,
                           "Event {RoutingKey} failed, scheduling retry {Next}/{Max} in {Delay}ms",
                           routingKey,
                           retryCount + 1,
                           MaxRetries,
                           RabbitMqTopology.RETRY_DELAY_MS);

        var properties = new BasicProperties
        {
            Persistent = true, Headers = new Dictionary<string, object?> {[RetryCountHeader] = retryCount + 1}
        };

        await channel.BasicPublishAsync(RabbitMqTopology.RETRY_EXCHANGE,
                                        routingKey,
                                        mandatory: false,
                                        basicProperties: properties,
                                        body: Encoding.UTF8.GetBytes(message),
                                        cancellationToken: ct);

        await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, ct);
    }

    private static int GetRetryCount(BasicDeliverEventArgs eventArgs)
    {
        if (eventArgs.BasicProperties.Headers is { } headers
            && headers.TryGetValue(RetryCountHeader, out object? value)
            && value is not null)
        {
            return Convert.ToInt32(value);
        }

        return 0;
    }
}
