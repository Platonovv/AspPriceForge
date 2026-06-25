using PriceForge.Application.Products;

namespace PriceForge.Application.Brokers;

public interface IProductEventStore
{
    Task SaveAsync(ProductCreatedEvent productEvent, CancellationToken ct);
}
