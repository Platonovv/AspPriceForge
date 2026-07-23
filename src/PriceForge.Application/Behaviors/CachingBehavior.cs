using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Cache;

namespace PriceForge.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDistributedCache _cache;
    private readonly ICacheVersion _cacheVersion;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    private static readonly SemaphoreSlim gate = new(1, 1);

    public CachingBehavior(IDistributedCache cache,
                           ICacheVersion cacheVersion,
                           ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cacheVersion);

        _logger = logger;
        _cache = cache;
        _cacheVersion = cacheVersion;
    }

    public async Task<TResponse> Handle(TRequest request,
                                        RequestHandlerDelegate<TResponse> next,
                                        CancellationToken cancellationToken)
    {
        if (request is not ICacheableRequest cacheableRequest)
        {
            return await next();
        }

        string version = await _cacheVersion.GetAsync(cancellationToken);
        string key = $"v{version}:{cacheableRequest.CacheKey}";

        string? cachedJson = await _cache.GetStringAsync(key, cancellationToken);
        if (cachedJson != null)
        {
            TResponse hit = JsonSerializer.Deserialize<TResponse>(cachedJson) ?? throw new InvalidOperationException();
            _logger.LogInformation("Cache HIT {key}", key);
            return hit;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            cachedJson = await _cache.GetStringAsync(key, cancellationToken);
            if (cachedJson != null)
            {
                TResponse hit = JsonSerializer.Deserialize<TResponse>(cachedJson)
                                ?? throw new InvalidOperationException();
                _logger.LogInformation("Cache HIT {key}", key);
                return hit;
            }

            TResponse response = await next();
            await _cache.SetStringAsync(key,
                                        JsonSerializer.Serialize(response),
                                        new DistributedCacheEntryOptions
                                        {
                                            AbsoluteExpirationRelativeToNow = cacheableRequest.Expiration
                                        },
                                        cancellationToken);
            _logger.LogInformation("Cache MISS {key}", key);
            return response;
        }
        finally
        {
            gate.Release();
        }
    }
}
