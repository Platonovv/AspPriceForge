using MediatR;

namespace PriceForge.Application.Carts;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartTotalQueryDto>
{
    private readonly CartPricingService _cartPricingService;

    public GetCartQueryHandler(CartPricingService cartPricingService)
    {
        ArgumentNullException.ThrowIfNull(cartPricingService);

        _cartPricingService = cartPricingService;
    }

    public async Task<CartTotalQueryDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        PricedCart cartPricing = await _cartPricingService.CalculatePrice(request.UserId,
                                                                          request.Tier,
                                                                          request.Region,
                                                                          cancellationToken);

        var cartQueryList = cartPricing.PricedCartItems
                                       .Select(x => new CartQueryDto(x.ProductName,
                                                                     x.Quantity,
                                                                     x.ItemsTotal,
                                                                     x.Currency))
                                       .ToList();

        return new CartTotalQueryDto(cartQueryList, cartPricing.TotalPrice, cartPricing.Currency);
    }
}
