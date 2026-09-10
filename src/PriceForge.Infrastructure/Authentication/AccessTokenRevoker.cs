using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PriceForge.Application.Authentication;

namespace PriceForge.Infrastructure.Authentication;

public sealed class AccessTokenRevoker : IAccessTokenRevoker
{
    private readonly JwtSettings _jwtSettings;
    private readonly IDistributedCache _distributedCache;

    public AccessTokenRevoker(IDistributedCache distributedCache, IOptions<JwtSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(distributedCache);

        _jwtSettings = options.Value;
        _distributedCache = distributedCache;
    }

    public Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
        => _distributedCache.SetStringAsync(AccessTokenRevocationKeys.RevokedBefore(userId.ToString()),
                                            DateTimeOffset.UtcNow
                                                          .ToUnixTimeSeconds()
                                                          .ToString(CultureInfo.InvariantCulture),
                                            new DistributedCacheEntryOptions
                                            {
                                                AbsoluteExpirationRelativeToNow
                                                    = TimeSpan.FromMinutes(_jwtSettings.AccessTokenMinutes)
                                            },
                                            cancellationToken);
}
