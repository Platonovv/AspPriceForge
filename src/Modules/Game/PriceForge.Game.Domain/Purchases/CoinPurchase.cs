namespace PriceForge.Game.Domain.Purchases;

public sealed class CoinPurchase
{
    public const int MaxIdempotencyKeyLength = 64;

    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid ProductId { get; }
    public long CoinPrice { get; }
    public string IdempotencyKey { get; } = null!;
    public DateTime CreatedAt { get; }

    public CoinPurchase(Guid userId, Guid productId, long coinPrice, string idempotencyKey, DateTime createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));
        }

        if (coinPrice <= 0)
        {
            throw new ArgumentException("CoinPrice must be positive", nameof(coinPrice));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength)
        {
            throw new ArgumentException($"IdempotencyKey must be 1-{MaxIdempotencyKeyLength} characters",
                                        nameof(idempotencyKey));
        }

        if (createdAt == DateTime.MinValue)
        {
            throw new ArgumentException("CreatedAt cannot be empty", nameof(createdAt));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        ProductId = productId;
        CoinPrice = coinPrice;
        IdempotencyKey = idempotencyKey;
        CreatedAt = createdAt;
    }

    private CoinPurchase()
    {
    }
}
