using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using PriceForge.Application.Carts;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.PromoCodes;
using PriceForge.Application.Transactions;
using PriceForge.Domain.Orders;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Application.Orders;

public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PlaceOrderHandler> _logger;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ITransactionRunner _transactionRunner;
    private readonly CartPricingService _cartPricingService;
    private readonly IPromoCodeRepository _promoCodeRepository;

    public PlaceOrderHandler(ILogger<PlaceOrderHandler> logger,
                             IOrderRepository orderRepository,
                             IPromoCodeRepository promoCodeRepository,
                             IOutboxRepository outboxRepository,
                             ICartRepository cartRepository,
                             CartPricingService cartPricingService,
                             ITransactionRunner transactionRunner)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cartRepository);
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(transactionRunner);
        ArgumentNullException.ThrowIfNull(cartPricingService);
        ArgumentNullException.ThrowIfNull(promoCodeRepository);

        _logger = logger;
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _outboxRepository = outboxRepository;
        _transactionRunner = transactionRunner;
        _cartPricingService = cartPricingService;
        _promoCodeRepository = promoCodeRepository;
    }

    public async Task<Guid> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        PricedCart priceCart = await _cartPricingService.CalculatePrice(request.UserId,
                                                                        request.Tier,
                                                                        request.Region,
                                                                        cancellationToken);
        if (priceCart.PricedCartItems.Count <= 0)
        {
            _logger.LogError("No items available for cart");
            throw new BusinessRuleException("No items available for cart");
        }

        decimal discountPercent = 0;

        DateTime utcNow = DateTime.UtcNow;

        PromoCode? promoCode = null;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            promoCode = await _promoCodeRepository.GetByCodeAsync(request.PromoCode, cancellationToken);

            if (promoCode == null)
            {
                _logger.LogError("Promo Code does not exist");
                throw new NotFoundException("Promo Code does not exist");
            }

            if (!promoCode.IsValid(priceCart.TotalPrice, utcNow).Valid)
            {
                _logger.LogError("Promo Code not valid");
                throw new BusinessRuleException("Promo Code does not valid");
            }

            discountPercent = promoCode.DiscountPercent;
        }

        var orderItems = priceCart.PricedCartItems
                                  .Select(x => new OrderItem(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice))
                                  .ToList();
        var order = new Order(orderItems,
                              request.UserId,
                              request.PromoCode,
                              discountPercent,
                              utcNow,
                              priceCart.Currency,
                              Status.Paid);

        var orderCreatedEvent = new OrderCreatedEvent(order.Id, utcNow);
        string productCreated = JsonSerializer.Serialize(orderCreatedEvent);
        var message = new OutboxMessage("order.placed", productCreated);

        Guid orderId = await _transactionRunner.RunAsync(async () =>
                                                         {
                                                             if (promoCode != null
                                                                 && !await _promoCodeRepository.TryIncrementUsageAsync(
                                                                         promoCode,
                                                                         cancellationToken))
                                                             {
                                                                 _logger.LogError("Promo Code conflict");
                                                                 throw new ConflictException("Promo Code conflict");
                                                             }

                                                             await _orderRepository.AddAsync(order, cancellationToken);
                                                             await _outboxRepository.AddAsync(message,
                                                                 cancellationToken);
                                                             await _orderRepository.SaveChangesAsync(cancellationToken);
                                                             return order.Id;
                                                         },
                                                         cancellationToken);

        try
        {
            await _cartRepository.ClearAsync(request.UserId);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "An error occured while clearing cart");
        }

        return orderId;
    }
}
