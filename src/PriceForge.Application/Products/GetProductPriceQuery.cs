using MediatR;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Products;

public record GetProductPriceQuery
    (Guid ProductId, int Quantity, CustomerTier Tier, Region Region) : IRequest<MoneyDto>, ICacheableRequest
{
    public string CacheKey => $"price:{ProductId}:{Quantity}:{Tier}:{Region}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
