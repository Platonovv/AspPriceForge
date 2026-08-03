using MediatR;

namespace PriceForge.Application.Carts;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand>
{
    private readonly ICartRepository _cartRepository;

    public RemoveFromCartHandler(ICartRepository cartRepository)
    {
        ArgumentNullException.ThrowIfNull(cartRepository);
        _cartRepository = cartRepository;
    }

    public async Task Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
        => await _cartRepository.RemoveItemAsync(request.UserId, request.ProductId);
}
