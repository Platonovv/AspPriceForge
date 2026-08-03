using PriceForge.Application.Categories;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Application.Promotions;
using PriceForge.Domain.Carts;
using PriceForge.Domain.Categories;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;

namespace PriceForge.Application.Carts;

public sealed class CartPricingService
{
    private readonly ICartRepository _cartRepository;
    private readonly PriceCalculator _priceCalculator;
    private readonly IProductRepository _productRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly ICategoryPromotionRepository _categoryPromotionRepository;

    public CartPricingService(ICartRepository cartRepository,
                              IProductRepository productRepository,
                              IPromotionRepository promotionRepository,
                              ICategoryPromotionRepository categoryPromotionRepository,
                              PriceCalculator priceCalculator)
    {
        ArgumentNullException.ThrowIfNull(cartRepository);
        ArgumentNullException.ThrowIfNull(priceCalculator);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(promotionRepository);
        ArgumentNullException.ThrowIfNull(categoryPromotionRepository);

        _cartRepository = cartRepository;
        _priceCalculator = priceCalculator;
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
        _categoryPromotionRepository = categoryPromotionRepository;
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

        var cartCartItemIds = cart.CartItems.Select(x => x.ProductId).ToList();

        DateTime currentTime = DateTime.UtcNow;

        List<Product> cartProducts = await _productRepository.GetByIdsAsync(cartCartItemIds, cancellationToken);

        List<Promotion> productPromotions
            = await _promotionRepository.GetActiveForProductsAsync(cartCartItemIds, currentTime, cancellationToken);

        List<ProductCategoryPromotion> productCategoryPromotions
            = await _categoryPromotionRepository.GetActiveForProductsAsync(
                  cartCartItemIds,
                  currentTime,
                  cancellationToken);

        var cartProductDictionary = cartProducts.ToDictionary(p => p.Id);
        var productPromotionsDictionary = productPromotions.ToDictionary(p => p.ProductId);
        var productCategoryPromotionsDictionary = productCategoryPromotions.GroupBy(x => x.ProductId)
                                                                           .ToDictionary(
                                                                               g => g.Key,
                                                                               g => g.Select(x => x.Promotion)
                                                                                   .ToList());

        foreach (CartItem cartItem in cart.CartItems)
        {
            if (!cartProductDictionary.TryGetValue(cartItem.ProductId, out Product? cartProduct))
            {
                continue;
            }

            var pricingContext = new PricingContext(cartProduct.BasePrice, cartItem.Quantity, tier, region);

            Money money = _priceCalculator.Calculate(pricingContext);

            productPromotionsDictionary.TryGetValue(cartItem.ProductId, out Promotion? promotion);
            productCategoryPromotionsDictionary.TryGetValue(cartItem.ProductId,
                                                            out List<CategoryPromotion>? categoryPromotions);

            money = _priceCalculator.CalculatePromotion(money, promotion, categoryPromotions, currentTime);

            var cartQueryDto = new PricedCartItem(cartProduct.Id,
                                                  cartProduct.Name,
                                                  cartItem.Quantity,
                                                  money.Amount,
                                                  money.Amount * cartItem.Quantity,
                                                  money.Currency);

            cartQueries.Add(cartQueryDto);
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
