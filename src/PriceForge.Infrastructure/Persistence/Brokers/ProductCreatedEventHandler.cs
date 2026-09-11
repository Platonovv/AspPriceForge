using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PriceForge.Application.Brokers;
using PriceForge.Application.Products;

namespace PriceForge.Infrastructure.Persistence.Brokers;

public sealed class ProductCreatedEventHandler : IIntegrationEventHandler
{
    private readonly IProductEventStore _eventStore;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public string EventType => IntegrationEventTypes.ProductCreated;

    public ProductCreatedEventHandler(IProductEventStore eventStore, ILogger<ProductCreatedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(logger);

        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task HandleAsync(string message, CancellationToken ct)
    {
        ProductCreatedEvent productCreatedEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(message)
                                                  ?? throw new InvalidOperationException(
                                                      "Failed to deserialize ProductCreatedEvent");
        try
        {
            await _eventStore.SaveAsync(productCreatedEvent, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogInformation("Duplicate event {EventId}, already processed", productCreatedEvent.Id);
        }
    }
}
