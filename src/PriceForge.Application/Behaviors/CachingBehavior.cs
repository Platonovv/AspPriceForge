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

        (bool Found, TResponse Value) cached = await TryGetFromCacheAsync(key, cancellationToken);

        if (cached.Found)
        {
            return cached.Value;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            cached = await TryGetFromCacheAsync(key, cancellationToken);

            if (cached.Found)
            {
                return cached.Value;
            }

            TResponse response = await next();
            await _cache.SetStringAsync(key,
                                        JsonSerializer.Serialize(response),
                                        new DistributedCacheEntryOptions
                                        {
                                            AbsoluteExpirationRelativeToNow = cacheableRequest.Expiration
                                        },
                                        cancellationToken);
            _logger.LogInformation("Cache MISS {Key}", key);
            return response;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<(bool Found, TResponse Value)> TryGetFromCacheAsync(string key,
                                                                          CancellationToken cancellationToken)
    {
        string? cachedJson = await _cache.GetStringAsync(key, cancellationToken);

        if (cachedJson == null)
        {
            return (false, default!);
        }

        try
        {
            TResponse? hit = JsonSerializer.Deserialize<TResponse>(cachedJson);

            if (hit != null)
            {
                _logger.LogInformation("Cache HIT {Key}", key);
                return (true, hit);
            }

            _logger.LogWarning("Cache {Key} contains null, removed", key);
        }
        catch (JsonException jsonException)
        {
            _logger.LogWarning(jsonException, "Cache {Key} is corrupted, removed", key);
        }

        await _cache.RemoveAsync(key, cancellationToken);
        return (false, default!);
    }
}
