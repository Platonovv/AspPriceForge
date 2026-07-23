using MediatR;
using PriceForge.Application.Cache;

namespace PriceForge.Application.Products;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>, ICacheableRequest
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
