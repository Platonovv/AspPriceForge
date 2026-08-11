using PriceForge.Application.Common;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Domain.Carts;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Carts;

public sealed class CartPricingService
{
    private readonly ICartRepository _cartRepository;

    private readonly IProductRepository _productRepository;
    private readonly ProductPricingService _productPricingService;

    public CartPricingService(ICartRepository cartRepository,
                              IProductRepository productRepository,
                              ProductPricingService productPricingService)
    {
        ArgumentNullException.ThrowIfNull(cartRepository);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(productPricingService);

        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _productPricingService = productPricingService;
    }

    public async Task<PricedCart> CalculatePrice(Guid userId,
                                                 CustomerTier tier,
                                                 Region region,
                                                 CancellationToken cancellationToken)
    {
        Cart cart = await _cartRepository.GetAsync(userId);

        var cartQueries = new List<PricedCartItem>();

        if (cart.CartItems.Count <= 0)
        {
            return new PricedCart(cartQueries,
                                  cartQueries.Sum(x => x.ItemsTotal),
                                  cartQueries.FirstOrDefault()?.Currency ?? Currency.RUB);
        }

        var productIds = cart.CartItems.Select(x => x.ProductId).ToList();

        List<Product> cartProducts = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var cartProductDictionary = cartProducts.ToDictionary(p => p.Id);

        var pricingItems = cart.CartItems
                               .Join(cartProducts,
                                     item => item.ProductId,
                                     product => product.Id,
                                     (item, product) => new PricingItem(product.Id, product.BasePrice, item.Quantity))
                               .ToList();

        IReadOnlyDictionary<Guid, Money> productPricing
            = await _productPricingService.CalculateProductPricingAsync(pricingItems, tier, region, cancellationToken);

        foreach (CartItem cartItem in cart.CartItems)
        {
            if (!cartProductDictionary.TryGetValue(cartItem.ProductId, out Product? cartProduct))
            {
                continue;
            }

            Money money = productPricing[cartItem.ProductId];
            cartQueries.Add(new PricedCartItem(cartItem.ProductId,
                                               cartProduct.Name,
                                               cartItem.Quantity,
                                               money.Amount,
                                               money.Amount * cartItem.Quantity,
                                               money.Currency));
        }

        Currency? cartCurrency = cartQueries.FirstOrDefault()?.Currency;

        if (cartQueries.Any(x => x.Currency != cartCurrency))
        {
            throw new ConflictException($"The cart's currency amount > 1.");
        }

        return new PricedCart(cartQueries,
                              cartQueries.Sum(x => x.ItemsTotal),
                              cartQueries.FirstOrDefault()?.Currency ?? Currency.RUB);
    }
}
