using Microsoft.Extensions.Caching.Distributed;
using PriceForge.Application.Cache;

namespace PriceForge.Application.Caching;

public sealed class CacheVersion : ICacheVersion
{
    private const string VersionKey = "cache-version";

    private readonly IDistributedCache _cache;

    public CacheVersion(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public async Task<string> GetAsync(CancellationToken ct)
    {
        string? version = await _cache.GetStringAsync(VersionKey, ct);

        if (version != null)
        {
            return version;
        }

        version = NewVersion();
        await _cache.SetStringAsync(VersionKey, version, ct);

        return version;
    }

    public Task BumpAsync(CancellationToken ct) => _cache.SetStringAsync(VersionKey, NewVersion(), ct);

    private static string NewVersion() => Guid.NewGuid().ToString("N")[..8];
}
