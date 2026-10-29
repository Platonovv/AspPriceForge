namespace PriceForge.Game.Domain.Wallets;

public sealed class WalletTransaction
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public long Amount { get; }
    public WalletTransactionReason Reason { get; }
    public DateTime CreatedAt { get; }

    public WalletTransaction(Guid userId, long amount, WalletTransactionReason reason, DateTime createdAt)
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

        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Reason = reason;
        CreatedAt = createdAt;
    }

    private WalletTransaction()
    {
    }
}
