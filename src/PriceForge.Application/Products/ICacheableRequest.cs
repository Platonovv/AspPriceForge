namespace PriceForge.Application.Products;

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
