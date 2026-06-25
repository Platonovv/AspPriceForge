using RabbitMQ.Client;

namespace PriceForge.Infrastructure.Persistence.Brokers;

internal static class RabbitMqTopology
{
    public const string EXCHANGE = "priceforge.events";
    public const string QUEUE = "product-events";
    public const string ROUTING_KEY = "product.#";

    public const string DEAD_LETTER_EXCHANGE = "priceforge.events.dlx";
    public const string DEAD_LETTER_QUEUE = "product-events.dlq";

    public const string RETRY_EXCHANGE = "priceforge.events.retry";
    public const string RETRY_QUEUE = "product-events.retry";
    public const int RETRY_DELAY_MS = 10000;

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(EXCHANGE, ExchangeType.Topic, durable: true, cancellationToken: ct);

        // 1. Рабочая очередь: при nack(requeue:false) сообщение уходит в DLX.
        await channel.QueueDeclareAsync(QUEUE,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: new Dictionary<string, object?>
                                        {
                                            ["x-dead-letter-exchange"] = DEAD_LETTER_EXCHANGE
                                        },
                                        cancellationToken: ct);
        await channel.QueueBindAsync(QUEUE, EXCHANGE, ROUTING_KEY, cancellationToken: ct);

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
