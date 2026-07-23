namespace PriceForge.Application.Cache;

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}
