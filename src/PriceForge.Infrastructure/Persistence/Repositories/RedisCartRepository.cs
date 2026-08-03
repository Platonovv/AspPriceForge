using Microsoft.Extensions.Logging;
using PriceForge.Application.Carts;
using PriceForge.Domain.Carts;
using StackExchange.Redis;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class RedisCartRepository : ICartRepository
{
    private const string CartKeyPrefix = "cart:";

    private readonly IDatabase _redisDb;
    private readonly ILogger<RedisCartRepository> _logger;

    public RedisCartRepository(IConnectionMultiplexer multiplexer, ILogger<RedisCartRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentNullException.ThrowIfNull(logger);

        _redisDb = multiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task AddItemAsync(Guid userId, Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity cannot be zero or negative", nameof(quantity));
        }

        string cartKeyPrefix = BuildKey(userId);
        await _redisDb.HashIncrementAsync(cartKeyPrefix, productId.ToString(), quantity);
        await _redisDb.KeyExpireAsync(cartKeyPrefix, DateTime.UtcNow.AddDays(1));
        _logger.LogInformation("Added {quantity} items to cart", quantity);
    }

    public async Task RemoveItemAsync(Guid userId, Guid productId)
    {
        await _redisDb.HashDeleteAsync(BuildKey(userId), productId.ToString());
        _logger.LogInformation("Remove item from cart {userId}", userId);
    }

    public async Task<Cart> GetAsync(Guid userId)
    {
        HashEntry[] hashEntries = await _redisDb.HashGetAllAsync(BuildKey(userId));

        var cart = new Cart(userId);
        foreach (HashEntry hashEntry in hashEntries)
        {
            cart.AddCartItem(Guid.Parse(hashEntry.Name.ToString()), (int) hashEntry.Value);
        }

        return cart;
    }

    public async Task ClearAsync(Guid userId)
    {
        await _redisDb.KeyDeleteAsync(BuildKey(userId));
        _logger.LogInformation("Clear cart {userId}", userId);
    }

    private static string BuildKey(Guid userId) => $"{CartKeyPrefix}{userId}";
}
