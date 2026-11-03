using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Game.Domain.Wallets;

namespace PriceForge.Game.UnitTests.Wallets;

[Trait("Category", "Unit")]
public class WalletTest
{
    private const int MaxCoinsPerSecond = 10;

    private const int MaxWindowSeconds = 30;

    private const string EarnKey = "earn-key";

    private static readonly DateTime CreatedAt = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime SpentAt = CreatedAt.AddMinutes(1);

    private static Wallet CreateWallet() => new(Guid.NewGuid(), CreatedAt);

    private static Wallet CreateWalletWith50Coins()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(5));
        return wallet;
    }

    [Fact]
    public void NewWallet_IsEmpty()
    {
        var userId = Guid.NewGuid();

        var wallet = new Wallet(userId, CreatedAt);

        Assert.Equal(userId, wallet.UserId);
        Assert.Equal(0, wallet.Balance);
        Assert.Equal(default, wallet.LastEarnedAtUtc);
        Assert.Empty(wallet.Transactions);
    }

    [Fact]
    public void NewWallet_WithEmptyUser_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Wallet(Guid.Empty, CreatedAt));
    }

    [Fact]
    public void Earn_WithinRate_AddsCoinsAndMovesWindow()
    {
        Wallet wallet = CreateWallet();
        DateTime now = CreatedAt.AddSeconds(5);

        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,now);

        Assert.Equal(50, wallet.Balance);
        Assert.Equal(now, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_TwoBatches_CountsWindowFromPreviousBatch()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(5));

        Assert.Throws<DomainException>(() => wallet.Earn(11, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(6)));
        Assert.Equal(50, wallet.Balance);
    }

    [Fact]
    public void Earn_FirstBatchRightAfterCreation_GetsFullWindow()
    {
        Wallet wallet = CreateWallet();

        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt);

        Assert.Equal(300, wallet.Balance);
        Assert.Equal(CreatedAt, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_FirstBatchAboveFullWindow_ThrowsAndKeepsState()
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Earn(301, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt));
        Assert.Equal(0, wallet.Balance);
        Assert.Equal(default, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_AboveRate_ThrowsAndKeepsState()
    {
        Wallet wallet = CreateWallet();
        DateTime firstBatchAt = CreatedAt.AddSeconds(5);
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,firstBatchAt);

        Assert.Throws<DomainException>(() => wallet.Earn(51, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,firstBatchAt.AddSeconds(5)));
        Assert.Equal(50, wallet.Balance);
        Assert.Equal(firstBatchAt, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_AfterLongPause_WindowIsCapped()
    {
        Wallet wallet = CreateWallet();
        DateTime weekLater = CreatedAt.AddDays(7);

        Assert.Throws<DomainException>(() => wallet.Earn(301, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,weekLater));

        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,weekLater);

        Assert.Equal(300, wallet.Balance);
        Assert.Equal(weekLater, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_AfterCappedBatch_NextWindowStartsFromIt()
    {
        Wallet wallet = CreateWallet();
        DateTime weekLater = CreatedAt.AddDays(7);
        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,weekLater);

        Assert.Throws<DomainException>(() => wallet.Earn(11, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,weekLater.AddSeconds(1)));
        Assert.Equal(300, wallet.Balance);
    }

    [Fact]
    public void Earn_FromThePast_Throws()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(5));

        Assert.Throws<DomainException>(() => wallet.Earn(1, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(4)));
        Assert.Equal(50, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Earn_NotPositive_Throws(long coins)
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Earn(coins, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(5)));
    }

    [Fact]
    public void Earn_WritesPositiveEarnEntry()
    {
        Wallet wallet = CreateWallet();
        DateTime now = CreatedAt.AddSeconds(5);

        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,now);

        WalletTransaction entry = Assert.Single(wallet.Transactions);
        Assert.Equal(wallet.UserId, entry.UserId);
        Assert.Equal(50, entry.Amount);
        Assert.Equal(WalletTransactionReason.Earn, entry.Reason);
        Assert.Equal(now, entry.CreatedAt);
        Assert.NotEqual(Guid.Empty, entry.Id);
    }

    [Fact]
    public void Earn_Rejected_WritesNoEntry()
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Earn(301, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt));
        Assert.Empty(wallet.Transactions);
    }

    [Fact]
    public void Spend_EnoughCoins_DecreasesBalance()
    {
        Wallet wallet = CreateWalletWith50Coins();

        wallet.Spend(30, SpentAt);

        Assert.Equal(20, wallet.Balance);
    }

    [Fact]
    public void Spend_WholeBalance_LeavesZero()
    {
        Wallet wallet = CreateWalletWith50Coins();

        wallet.Spend(50, SpentAt);

        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public void Spend_NotEnoughCoins_ThrowsAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWith50Coins();

        Assert.Throws<DomainException>(() => wallet.Spend(51, SpentAt));
        Assert.Equal(50, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Spend_NotPositive_Throws(long coins)
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Spend(coins, SpentAt));
    }

    [Fact]
    public void Spend_EmptyTime_ThrowsDomainExceptionAndKeepsState()
    {
        Wallet wallet = CreateWalletWith50Coins();

        Assert.Throws<DomainException>(() => wallet.Spend(10, DateTime.MinValue));
        Assert.Equal(50, wallet.Balance);
        Assert.Single(wallet.Transactions);
    }

    [Fact]
    public void Spend_WritesNegativePurchaseEntry()
    {
        Wallet wallet = CreateWalletWith50Coins();

        wallet.Spend(30, SpentAt);

        WalletTransaction entry = wallet.Transactions[^1];
        Assert.Equal(-30, entry.Amount);
        Assert.Equal(WalletTransactionReason.Purchase, entry.Reason);
        Assert.Equal(SpentAt, entry.CreatedAt);
    }

    [Fact]
    public void Spend_NotEnoughCoins_WritesNoEntry()
    {
        Wallet wallet = CreateWalletWith50Coins();

        Assert.Throws<DomainException>(() => wallet.Spend(51, SpentAt));
        Assert.Single(wallet.Transactions);
    }

    [Fact]
    public void Journal_SumOfAmounts_EqualsBalance()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt);
        wallet.Spend(120, CreatedAt.AddSeconds(1));
        wallet.Earn(100, MaxCoinsPerSecond, MaxWindowSeconds, EarnKey,CreatedAt.AddSeconds(20));
        wallet.Spend(30, CreatedAt.AddSeconds(21));

        Assert.Equal(4, wallet.Transactions.Count);
        Assert.Equal(wallet.Balance, wallet.Transactions.Sum(t => t.Amount));
    }

    [Fact]
    public void Earn_WritesKeyToJournal()
    {
        Wallet wallet = CreateWalletWith50Coins();

        Assert.Equal(EarnKey, Assert.Single(wallet.Transactions).IdempotencyKey);
    }

    [Fact]
    public void Spend_WritesNoKeyToJournal()
    {
        Wallet wallet = CreateWalletWith50Coins();

        wallet.Spend(10, SpentAt);

        Assert.Null(wallet.Transactions[^1].IdempotencyKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void JournalEntry_EmptyKey_Throws(string key)
    {
        Assert.Throws<ArgumentException>(() => CreateJournalEntry(key));
    }

    [Fact]
    public void JournalEntry_TooLongKey_Throws()
    {
        string key = new('k', WalletTransaction.MaxIdempotencyKeyLength + 1);

        Assert.Throws<ArgumentException>(() => CreateJournalEntry(key));
    }

    [Fact]
    public void JournalEntry_KeyOfMaxLength_IsAccepted()
    {
        string key = new('k', WalletTransaction.MaxIdempotencyKeyLength);

        Assert.Equal(key, CreateJournalEntry(key).IdempotencyKey);
    }

    private static WalletTransaction CreateJournalEntry(string key)
        => new(Guid.NewGuid(), 10, key, WalletTransactionReason.Earn, CreatedAt);
}
