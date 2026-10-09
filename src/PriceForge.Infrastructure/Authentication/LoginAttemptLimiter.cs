using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PriceForge.Application.Authentication;
using StackExchange.Redis;

namespace PriceForge.Infrastructure.Authentication;

internal sealed class LoginAttemptLimiter : ILoginAttemptLimiter
{
    private const string KeyPrefix = "login:fail:";

    private readonly IDatabase _database;
    private readonly LoginAttemptSettings _settings;

    public LoginAttemptLimiter(IConnectionMultiplexer multiplexer, IOptions<LoginAttemptSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(multiplexer);

        _database = multiplexer.GetDatabase();
        _settings = options.Value;
    }

    public async Task<bool> IsBlockedAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string redisKey = RedisKey(key);

        RedisValue redisValue = await _database.StringGetAsync(redisKey);

        if (!redisValue.HasValue)
        {
            return false;
        }

        return (long) redisValue >= _settings.MaxFailedAttempts;
    }

    public async Task RegisterFailureAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string redisKey = RedisKey(key);

        await _database.StringIncrementAsync(redisKey);

        await _database.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(_settings.WindowMinutes), ExpireWhen.HasNoExpiry);
    }

    public async Task ResetAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string redisKey = RedisKey(key);

        await _database.KeyDeleteAsync(redisKey);
    }

    private static string RedisKey(string email)
        => KeyPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email)));
}
