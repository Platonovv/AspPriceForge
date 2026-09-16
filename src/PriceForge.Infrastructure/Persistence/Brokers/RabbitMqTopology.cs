using RabbitMQ.Client;
using PriceForge.Application.Brokers;

namespace PriceForge.Infrastructure.Persistence.Brokers;

internal static class RabbitMqTopology
{
    public const string EXCHANGE = "priceforge.events";
    public const string PRODUCT_QUEUE = "product-events";
    public const string ROUTING_KEY = "product.#";

    public const string DEAD_LETTER_EXCHANGE = "priceforge.events.dlx";
    public const string DEAD_LETTER_QUEUE = "product-events.dlq";

    public const string EMAIL_DEAD_LETTER_EXCHANGE = "priceforge.events.email.dlx";

    public const string EMAIL_DEAD_LETTER_QUEUE = "email-events.dlq";
    public const string EMAIL_QUEUE = "email-events";
    public const string EMAIL_CONFIRMATION_ROUTING_KEY = IntegrationEventTypes.EmailConfirmationRequested;
    public const string PASSWORD_RESET_ROUTING_KEY = IntegrationEventTypes.PasswordResetRequested;

    public const string ALTERNATE_EXCHANGE = "priceforge.events.unrouted";
    public const string ALTERNATE_QUEUE = "unrouted-events";

    public const string RETRY_EXCHANGE = "priceforge.events.retry";
    public const string RETRY_QUEUE = "priceforge.events.retry";
    public const int RETRY_DELAY_MS = 10000;

    public static async Task DeclareExchangesAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(ALTERNATE_EXCHANGE,
                                           ExchangeType.Fanout,
                                           durable: true,
                                           cancellationToken: ct);

        await channel.ExchangeDeclareAsync(EXCHANGE,
                                           ExchangeType.Topic,
                                           durable: true,
                                           arguments: new Dictionary<string, object?>
                                           {
                                               ["alternate-exchange"] = ALTERNATE_EXCHANGE
                                           },
                                           cancellationToken: ct);
    }

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await DeclareExchangesAsync(channel, ct);

        // 1. Рабочая очередь: при nack(requeue:false) сообщение уходит в DLX.
        await channel.QueueDeclareAsync(PRODUCT_QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: new Dictionary<string, object?>
                                        {
                                            ["x-dead-letter-exchange"] = DEAD_LETTER_EXCHANGE
                                        },
                                        cancellationToken: ct);
        // Email очередь
        await channel.QueueDeclareAsync(EMAIL_QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: new Dictionary<string, object?>
                                        {
                                            ["x-dead-letter-exchange"] = EMAIL_DEAD_LETTER_EXCHANGE
                                        },
                                        cancellationToken: ct);
        // Для потеряшек очерель
        await channel.QueueDeclareAsync(ALTERNATE_QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        cancellationToken: ct);

        await channel.QueueBindAsync(PRODUCT_QUEUE, EXCHANGE, ROUTING_KEY, cancellationToken: ct);
        await channel.QueueBindAsync(EMAIL_QUEUE, EXCHANGE, EMAIL_CONFIRMATION_ROUTING_KEY, cancellationToken: ct);
        await channel.QueueBindAsync(EMAIL_QUEUE, EXCHANGE, PASSWORD_RESET_ROUTING_KEY, cancellationToken: ct);
        await channel.QueueBindAsync(ALTERNATE_QUEUE, ALTERNATE_EXCHANGE, "", cancellationToken: ct);

        // 2. Dead-letter: финальная парковка ядовитых / исчерпавших повторы.
        await channel.ExchangeDeclareAsync(DEAD_LETTER_EXCHANGE,
                                           ExchangeType.Fanout,
                                           durable: true,
                                           cancellationToken: ct);
        await channel.QueueDeclareAsync(DEAD_LETTER_QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        cancellationToken: ct);
        await channel.QueueBindAsync(DEAD_LETTER_QUEUE, DEAD_LETTER_EXCHANGE, routingKey: "", cancellationToken: ct);

        // Email
        await channel.ExchangeDeclareAsync(EMAIL_DEAD_LETTER_EXCHANGE,
                                           ExchangeType.Fanout,
                                           durable: true,
                                           cancellationToken: ct);
        await channel.QueueDeclareAsync(EMAIL_DEAD_LETTER_QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        cancellationToken: ct);
        await channel.QueueBindAsync(EMAIL_DEAD_LETTER_QUEUE,
                                     EMAIL_DEAD_LETTER_EXCHANGE,
                                     routingKey: "",
                                     cancellationToken: ct);

        // 3. Retry: полежать TTL, затем dead-letter ОБРАТНО в основной exchange -> вернётся в рабочую очередь.
        await channel.ExchangeDeclareAsync(RETRY_EXCHANGE, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(RETRY_QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: new Dictionary<string, object?>
                                        {
                                            ["x-message-ttl"] = RETRY_DELAY_MS,
                                            ["x-dead-letter-exchange"] = EXCHANGE
                                        },
                                        cancellationToken: ct);
        await channel.QueueBindAsync(RETRY_QUEUE, RETRY_EXCHANGE, routingKey: "", cancellationToken: ct);
    }
}
