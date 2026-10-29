using MediatR;
using Microsoft.Extensions.Logging;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Application.Products.Images;
using PriceForge.Domain.Images;

namespace PriceForge.Application.Images;

public sealed class AssignImageHandler : IRequestHandler<AssignImageCommand>
{
    private readonly IImageStorage _imageStorage;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _imageRepository;
    private readonly ILogger<AssignImageHandler> _logger;

    public AssignImageHandler(IProductRepository productRepository,
                              IProductImageRepository imageRepository,
                              IImageStorage imageStorage,
                              ILogger<AssignImageHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(imageStorage);
        ArgumentNullException.ThrowIfNull(imageRepository);
        ArgumentNullException.ThrowIfNull(productRepository);

        _logger = logger;
        _imageStorage = imageStorage;
        _imageRepository = imageRepository;
        _productRepository = productRepository;
    }

    public async Task Handle(AssignImageCommand request, CancellationToken cancellationToken)
    {
        bool productExists = await _productRepository.ExistsAsync(request.ProductId, cancellationToken);
        if (!productExists)
        {
            throw new NotFoundException("Product not found");
        }

        ImageFormat? imageFormat = await ImageSignature.DetectAsync(request.Content, cancellationToken);

        if (imageFormat == null)
        {
            throw new BusinessRuleException("Only JPEG and PNG are accepted");
        }

        string key = $"products/{request.ProductId}/{Guid.NewGuid()}.{imageFormat.Extension}";

        ProductImage? productImage = await _imageRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        string? oldKey = productImage?.Key;

        await _imageStorage.SaveAsync(key, request.Content, imageFormat.ContentType, cancellationToken);

        if (productImage != null)
        {
            productImage.Replace(key, imageFormat.ContentType, request.SizeBytes);
        }
        else
        {
            await _imageRepository.AddAsync(
                new ProductImage(request.ProductId, key, imageFormat.ContentType, request.SizeBytes),
                cancellationToken);
        }

        await _imageRepository.SaveChangesAsync(cancellationToken);

        if (oldKey != null)
        {
            try
            {
                await _imageStorage.DeleteAsync(oldKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old image {Key}", oldKey);
            }
        }
    }
}
