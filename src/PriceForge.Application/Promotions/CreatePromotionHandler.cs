using MediatR;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;

namespace PriceForge.Application.Promotions;

public class CreatePromotionHandler : IRequestHandler<CreatePromotionCommand, Guid>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IProductRepository _productRepository;

    public CreatePromotionHandler(IPromotionRepository promotionRepository, IProductRepository productRepository)
    {
        ArgumentNullException.ThrowIfNull(promotionRepository);
        ArgumentNullException.ThrowIfNull(productRepository);

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

        await _promotionRepository.AddAsync(newPromotion, cancellationToken);
        await _promotionRepository.SaveChangesAsync(cancellationToken);

        return newPromotion.Id;
    }
}
