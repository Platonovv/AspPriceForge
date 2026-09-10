using PriceForge.Application.Notifications;
using StackExchange.Redis;

namespace PriceForge.Infrastructure.Notifications;

internal sealed class EmailRateLimiter : IEmailRateLimiter
{
    private const string KeyPrefix = "ratelimit:email:";

    private readonly IDatabase _database;

    public EmailRateLimiter(IConnectionMultiplexer multiplexer)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);

        _database = multiplexer.GetDatabase();
    }

    public async Task<bool> TryConsumeAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken)
    {
        string redisKey = KeyPrefix + key;

        long attempts = await _database.StringIncrementAsync(redisKey);

        await _database.KeyExpireAsync(redisKey, window, ExpireWhen.HasNoExpiry);

        return attempts <= limit;
    }
}
