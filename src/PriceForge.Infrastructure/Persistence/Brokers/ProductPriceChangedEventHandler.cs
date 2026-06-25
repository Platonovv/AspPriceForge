using System.Text.Json;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using PriceForge.Application.Notifications;
using PriceForge.Application.Products;

namespace PriceForge.Infrastructure.Persistence.Brokers;

public sealed class ProductPriceChangedEventHandler : IIntegrationEventHandler
{
    private readonly IEnumerable<INotificationChannel> _notificationChannels;
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public string EventType => "product.price.changed";

    public ProductPriceChangedEventHandler(IEnumerable<INotificationChannel> notificationChannels,
                                           ILogger<ProductPriceChangedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(notificationChannels);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _notificationChannels = notificationChannels;
    }

    public async Task HandleAsync(string message, CancellationToken ct)
    {
        ProductPriceChangedEvent productPriceChangedEvent
            = JsonSerializer.Deserialize<ProductPriceChangedEvent>(message)
              ?? throw new InvalidOperationException("Failed to deserialize ProductPriceChangedEvent");

        foreach (INotificationChannel channel in _notificationChannels)
        {
            try
            {
                await channel.SendAsync("Цена изменилась",
                                        $"Продукт {productPriceChangedEvent.ProductId}: новая цена {productPriceChangedEvent.NewPrice} {productPriceChangedEvent.Currency}",
                                        ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Канал {Channel} упал", channel.GetType().Name);
            }
        }
    }
}
