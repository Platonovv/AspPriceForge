using System.Text.Json;
using MediatR;
using PriceForge.Application.Brokers;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(IProductRepository productRepository, IOutboxRepository outboxRepository)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(outboxRepository);
        _outboxRepository = outboxRepository;
        _productRepository = productRepository;
    }

    public async Task<Guid> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var sku = new Sku(command.Sku);
        Currency currency = Enum.Parse<Currency>(command.Currency);
        var money = new Money(command.Price, currency);
        var product = new Product(sku, command.Name, money);

        var productCreatedEvent = new ProductCreatedEvent(product.Id, command.Name, command.Sku, DateTime.UtcNow);
        string productCreated = JsonSerializer.Serialize(productCreatedEvent);
        var message = new OutboxMessage("product.created", productCreated);

        await _productRepository.AddAsync(product, cancellationToken);
        await _outboxRepository.AddAsync(message, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
