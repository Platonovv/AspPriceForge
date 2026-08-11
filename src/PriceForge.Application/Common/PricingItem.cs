using PriceForge.Domain.Common;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Common;

public record PricingItem(Guid ProductId, Money BasePrice, int Quantity);
