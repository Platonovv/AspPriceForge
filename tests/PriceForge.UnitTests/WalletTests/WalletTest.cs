using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Wallets;

namespace PriceForge.UnitTests.WalletTests;

[Trait("Category", "Unit")]
public class WalletTest
{
    private const int MaxCoinsPerSecond = 10;

    private const int MaxWindowSeconds = 30;

    private static readonly DateTime CreatedAt = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    private static Wallet CreateWallet() => new(Guid.NewGuid(), CreatedAt);

    [Fact]
    public void NewWallet_IsEmpty()
    {
        var userId = Guid.NewGuid();

        var wallet = new Wallet(userId, CreatedAt);

        Assert.Equal(userId, wallet.UserId);
        Assert.Equal(0, wallet.Balance);
        Assert.Equal(default, wallet.LastEarnedAtUtc);
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

        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, now);

        Assert.Equal(50, wallet.Balance);
        Assert.Equal(now, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_TwoBatches_CountsWindowFromPreviousBatch()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(5));

        Assert.Throws<DomainException>(() => wallet.Earn(11, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(6)));
        Assert.Equal(50, wallet.Balance);
    }

    [Fact]
    public void Earn_FirstBatchRightAfterCreation_GetsFullWindow()
    {
        Wallet wallet = CreateWallet();

        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt);

        Assert.Equal(300, wallet.Balance);
        Assert.Equal(CreatedAt, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_FirstBatchAboveFullWindow_ThrowsAndKeepsState()
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Earn(301, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt));
        Assert.Equal(0, wallet.Balance);
        Assert.Equal(default, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_AboveRate_ThrowsAndKeepsState()
    {
        Wallet wallet = CreateWallet();
        DateTime firstBatchAt = CreatedAt.AddSeconds(5);
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, firstBatchAt);

        Assert.Throws<DomainException>(() => wallet.Earn(51, MaxCoinsPerSecond, MaxWindowSeconds, firstBatchAt.AddSeconds(5)));
        Assert.Equal(50, wallet.Balance);
        Assert.Equal(firstBatchAt, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_AfterLongPause_WindowIsCapped()
    {
        Wallet wallet = CreateWallet();
        DateTime weekLater = CreatedAt.AddDays(7);

        Assert.Throws<DomainException>(() => wallet.Earn(301, MaxCoinsPerSecond, MaxWindowSeconds, weekLater));

        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, weekLater);

        Assert.Equal(300, wallet.Balance);
        Assert.Equal(weekLater, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public void Earn_AfterCappedBatch_NextWindowStartsFromIt()
    {
        Wallet wallet = CreateWallet();
        DateTime weekLater = CreatedAt.AddDays(7);
        wallet.Earn(300, MaxCoinsPerSecond, MaxWindowSeconds, weekLater);

        Assert.Throws<DomainException>(() => wallet.Earn(11, MaxCoinsPerSecond, MaxWindowSeconds, weekLater.AddSeconds(1)));
        Assert.Equal(300, wallet.Balance);
    }

    [Fact]
    public void Earn_FromThePast_Throws()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(5));

        Assert.Throws<DomainException>(() => wallet.Earn(1, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(4)));
        Assert.Equal(50, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Earn_NotPositive_Throws(long coins)
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Earn(coins, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(5)));
    }

    [Fact]
    public void Spend_EnoughCoins_DecreasesBalance()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(5));

        wallet.Spend(30);

        Assert.Equal(20, wallet.Balance);
    }

    [Fact]
    public void Spend_WholeBalance_LeavesZero()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(5));

        wallet.Spend(50);

        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public void Spend_NotEnoughCoins_ThrowsAndKeepsBalance()
    {
        Wallet wallet = CreateWallet();
        wallet.Earn(50, MaxCoinsPerSecond, MaxWindowSeconds, CreatedAt.AddSeconds(5));

        Assert.Throws<DomainException>(() => wallet.Spend(51));
        Assert.Equal(50, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Spend_NotPositive_Throws(long coins)
    {
        Wallet wallet = CreateWallet();

        Assert.Throws<DomainException>(() => wallet.Spend(coins));
    }
}
