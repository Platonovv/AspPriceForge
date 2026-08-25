using MediatR;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Domain.Images;

namespace PriceForge.Application.Images;

public sealed class GetImageHandler : IRequestHandler<GetImageQuery, ImageDto>
{
    private readonly IImageStorage _imageStorage;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _imageRepository;

    public GetImageHandler(IProductRepository productRepository,
                           IProductImageRepository imageRepository,
                           IImageStorage imageStorage)
    {
        ArgumentNullException.ThrowIfNull(imageStorage);
        ArgumentNullException.ThrowIfNull(imageRepository);
        ArgumentNullException.ThrowIfNull(productRepository);

        _imageStorage = imageStorage;
        _imageRepository = imageRepository;
        _productRepository = productRepository;
    }

    public async Task<ImageDto> Handle(GetImageQuery request, CancellationToken cancellationToken)
    {
        bool productExists = await _productRepository.ExistsAsync(request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new NotFoundException("Product not found");
        }

        ProductImage? productImage = await _imageRepository.GetByProductIdAsync(request.ProductId, cancellationToken);

        if (productImage == null)
        {
            throw new NotFoundException("Product image not found");
        }

        string downloadUrl = _imageStorage.GetDownloadUrl(productImage.Key);

        return new ImageDto(downloadUrl, productImage.ContentType);
    }
}
