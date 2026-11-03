namespace PriceForge.Game.Domain.Wallets;

public sealed class WalletTransaction
{
    public const int MaxIdempotencyKeyLength = 64;

    public Guid Id { get; }
    public Guid UserId { get; }
    public long Amount { get; }
    public WalletTransactionReason Reason { get; }
    public DateTime CreatedAt { get; }
    public string? IdempotencyKey { get; }

    public WalletTransaction(Guid userId,
                             long amount,
                             string? idempotencyKey,
                             WalletTransactionReason reason,
                             DateTime createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        }

        if (amount == 0)
        {
            throw new ArgumentException("Amount cannot be zero", nameof(amount));
        }

        if (createdAt == DateTime.MinValue)
        {
            throw new ArgumentException("CreatedAt cannot be empty", nameof(createdAt));
        }

        if (idempotencyKey != null
            && (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength))
        {
            throw new ArgumentException($"IdempotencyKey must be 1-{MaxIdempotencyKeyLength} characters",
                                        nameof(idempotencyKey));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Reason = reason;
        CreatedAt = createdAt;
        IdempotencyKey = idempotencyKey;
    }

    private WalletTransaction()
    {
    }
}
