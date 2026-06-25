using MediatR;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(IProductRepository productRepository)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product == null)
        {
            return null;
        }

        return new ProductDto(product.Id,
                              product.Sku.Value,
                              product.Name,
                              product.BasePrice.Amount,
                              product.BasePrice.Currency.ToString());
    }
}
