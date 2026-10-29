using System.Text.Json;
using MediatR;
using PriceForge.Application.Brokers;
using PriceForge.Application.Cache;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public sealed class UpdateProductPriceHandler : IRequestHandler<UpdateProductPriceCommand>
{
    private readonly PublicPriceRecalculator _priceRecalculator;
    private readonly IProductRepository _productRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ICacheVersion _cacheVersion;

    public UpdateProductPriceHandler(ICacheVersion cacheVersion,
                                     IProductRepository productRepository,
                                     IOutboxRepository outboxRepository,
                                     PublicPriceRecalculator priceRecalculator)
    {
        ArgumentNullException.ThrowIfNull(cacheVersion);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(priceRecalculator);

        _cacheVersion = cacheVersion;
        _outboxRepository = outboxRepository;
        _productRepository = productRepository;
        _priceRecalculator = priceRecalculator;
    }

    public async Task Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _productRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product not found");
        }

        Currency currency = Enum.Parse<Currency>(request.Currency);

        var money = new Money(request.NewPrice, currency);

        product.ChangeBasePrice(money);

        await _priceRecalculator.RecalculateAsync([product.Id], cancellationToken);

        var productPriceChangedEvent = new ProductPriceChangedEvent(Guid.NewGuid(),
                                                                    product.Id,
                                                                    product.BasePrice.Amount,
                                                                    product.BasePrice.Currency,
                                                                    DateTime.UtcNow);
        string productPriceChanged = JsonSerializer.Serialize(productPriceChangedEvent);
        var message = new OutboxMessage(IntegrationEventTypes.ProductPriceChanged, productPriceChanged);

        await _outboxRepository.AddAsync(message, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);
        await _cacheVersion.BumpAsync(cancellationToken);
    }
}
