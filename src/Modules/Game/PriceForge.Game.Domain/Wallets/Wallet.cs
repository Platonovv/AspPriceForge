using PriceForge.BuildingBlocks.Exceptions;

namespace PriceForge.Game.Domain.Wallets;

public sealed class Wallet
{
    public Guid UserId { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastEarnedAtUtc { get; private set; }
    public long Balance { get; private set; }

    private readonly List<WalletTransaction> _transactions = new();
    public IReadOnlyList<WalletTransaction> Transactions => _transactions;

    public Wallet(Guid userId, DateTime createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        }

        if (createdAt == DateTime.MinValue)
        {
            throw new ArgumentException("Created cannot be empty", nameof(createdAt));
        }

        UserId = userId;
        CreatedAt = createdAt;
    }

    private Wallet()
    {
    }

    public void Earn(long earnMoney, int maxCoinsPerSecond, int maxEarnWindowSeconds, DateTime earnedAt)
    {
        if (earnMoney <= 0)
        {
            throw new DomainException("EarnMoney cannot be negative or zero");
        }

        if (earnedAt == DateTime.MinValue)
        {
            throw new DomainException("EarnedAt cannot be empty");
        }

        DateTime lastEarnedAt = LastEarnedAtUtc;

        if (lastEarnedAt == default)
        {
            lastEarnedAt = earnedAt - TimeSpan.FromSeconds(maxEarnWindowSeconds);
        }

        if (earnedAt < lastEarnedAt)
        {
            throw new DomainException("EarnedAt cannot be in the past");
        }

        TimeSpan sinceLastEarn = earnedAt - lastEarnedAt;
        double windowSeconds = Math.Min(sinceLastEarn.TotalSeconds, maxEarnWindowSeconds);
        double limit = maxCoinsPerSecond * windowSeconds;

        if (earnMoney > limit)
        {
            throw new DomainException("EarnMoney cannot exceed maximum amount of money");
        }

        Balance += earnMoney;
        LastEarnedAtUtc = earnedAt;

        WalletTransaction walletTransaction = new WalletTransaction(UserId, earnMoney, WalletTransactionReason.Earn, earnedAt);
        _transactions.Add(walletTransaction);
    }

    public void Spend(long spendMoney, DateTime spentAt)
    {
        if (spendMoney <= 0)
        {
            throw new DomainException("SpendMoney cannot be negative or zero");
        }

        if (spendMoney > Balance)
        {
            throw new DomainException("Insufficient funds");
        }

        if (spentAt == DateTime.MinValue)
        {
            throw new DomainException("SpendAt cannot be empty");
        }

        Balance -= spendMoney;

        WalletTransaction walletTransaction = new WalletTransaction(UserId, -spendMoney, WalletTransactionReason.Purchase, spentAt);
        _transactions.Add(walletTransaction);
    }
}
