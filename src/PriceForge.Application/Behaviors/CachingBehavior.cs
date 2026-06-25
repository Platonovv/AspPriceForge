using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Products;

namespace PriceForge.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    private static readonly SemaphoreSlim gate = new(1, 1);

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request,
                                        RequestHandlerDelegate<TResponse> next,
                                        CancellationToken cancellationToken)
    {
        if (request is not ICacheableRequest cacheableRequest)
        {
            return await next();
        }

        string? cachedJson = await _cache.GetStringAsync(cacheableRequest.CacheKey, cancellationToken);
        if (cachedJson != null)
        {
            TResponse hit = JsonSerializer.Deserialize<TResponse>(cachedJson) ?? throw new InvalidOperationException();
            _logger.LogInformation("Cache HIT {key}", cacheableRequest.CacheKey);
            return hit;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            cachedJson = await _cache.GetStringAsync(cacheableRequest.CacheKey, cancellationToken);
            if (cachedJson != null)
            {
                TResponse hit = JsonSerializer.Deserialize<TResponse>(cachedJson)
                                ?? throw new InvalidOperationException();
                _logger.LogInformation("Cache HIT {key}", cacheableRequest.CacheKey);
                return hit;
            }

            TResponse response = await next();
            await _cache.SetStringAsync(cacheableRequest.CacheKey,
                                        JsonSerializer.Serialize(response),
                                        new DistributedCacheEntryOptions
                                        {
                                            AbsoluteExpirationRelativeToNow = cacheableRequest.Expiration
                                        },
                                        cancellationToken);
            _logger.LogInformation("Cache MISS {key}", cacheableRequest.CacheKey);
            return response;
        }
        finally
        {
            gate.Release();
        }
    }
}
