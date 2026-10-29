using MediatR;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Products;

namespace PriceForge.Application.Carts;

public sealed class AddToCartHandler : IRequestHandler<AddToCartCommand>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;

    public AddToCartHandler(ICartRepository cartRepository, IProductRepository productRepository)
    {
        ArgumentNullException.ThrowIfNull(cartRepository);
        ArgumentNullException.ThrowIfNull(productRepository);

        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (!await _productRepository.ExistsAsync(request.ProductId, cancellationToken))
        {
            throw new NotFoundException("Product not found");
        }

        await _cartRepository.AddItemAsync(request.UserId, request.ProductId, request.Quantity);
    }
}
