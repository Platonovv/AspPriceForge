using MediatR;
using PriceForge.Application.Cache;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Application.Transactions;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;

namespace PriceForge.Application.Promotions;

public class CreatePromotionHandler : IRequestHandler<CreatePromotionCommand, Guid>
{
    private readonly ICacheVersion _cacheVersion;
    private readonly ITransactionRunner _runner;
    private readonly IProductRepository _productRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly PublicPriceRecalculator _priceRecalculator;

    public CreatePromotionHandler(ICacheVersion cacheVersion,
                                  IPromotionRepository promotionRepository,
                                  IProductRepository productRepository,
                                  ITransactionRunner runner,
                                  PublicPriceRecalculator priceRecalculator)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(cacheVersion);
        ArgumentNullException.ThrowIfNull(priceRecalculator);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(promotionRepository);

        _runner = runner;
        _cacheVersion = cacheVersion;
        _priceRecalculator = priceRecalculator;
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
    }

    public async Task<Guid> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product not found");
        }

        if (await _promotionRepository.HasOverlappingAsync(request.ProductId,
                                                           request.StartAt,
                                                           request.EndAt,
                                                           cancellationToken))
        {
            throw new ConflictException("Product overlapping promotion");
        }

        var newPromotion = new Promotion(request.ProductId, request.Percent, request.StartAt, request.EndAt);

        await _runner.RunAsync(async () =>
                               {
                                   await _promotionRepository.AddAsync(newPromotion, cancellationToken);
                                   await _promotionRepository.SaveChangesAsync(cancellationToken);
                                   await _priceRecalculator.RecalculateAsync([request.ProductId], cancellationToken);
                                   await _promotionRepository.SaveChangesAsync(cancellationToken);
                               },
                               cancellationToken);

        await _cacheVersion.BumpAsync(cancellationToken);

        return newPromotion.Id;
    }
}
