using Microsoft.Extensions.Options;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Wallets;
using PriceForge.Domain.Wallets;

namespace PriceForge.UnitTests.WalletTests;

[Trait("Category", "Unit")]
public class EarnCoinsHandlerTest
{
    private const int MaxCoinsPerSecond = 10;
    private const int MaxWindowSeconds = 30;

    private static readonly DateTimeOffset StartedAt = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly ManualTimeProvider _clock = new(StartedAt);
    private readonly InMemoryWalletRepository _repository = new();

    private EarnCoinsHandler CreateHandler(int maxCoinsPerSecond = MaxCoinsPerSecond)
    {
        var settings = new WalletSettings {MaxCoinsPerSecond = maxCoinsPerSecond, MaxEarnWindowSeconds = MaxWindowSeconds};
        return new EarnCoinsHandler(Options.Create(settings), _repository, _clock);
    }

    private Task<long> Earn(long coins) => CreateHandler().Handle(new EarnCoinsCommand(_userId, coins), CancellationToken.None);

    [Fact]
    public async Task NoWallet_CreatesWalletAndAcceptsFullFirstWindow()
    {
        long balance = await Earn(300);

        Assert.Equal(300, balance);
        Wallet wallet = Assert.Single(_repository.Saved);
        Assert.Equal(_userId, wallet.UserId);
        Assert.Equal(StartedAt.UtcDateTime, wallet.CreatedAt);
        Assert.Equal(StartedAt.UtcDateTime, wallet.LastEarnedAtUtc);
    }

    [Fact]
    public async Task NoWallet_FirstBatchAboveFullWindow_ThrowsAndSavesNothing()
    {
        await Assert.ThrowsAsync<DomainException>(() => Earn(301));

        Assert.Equal(0, _repository.SaveCalls);
        Assert.Empty(_repository.Saved);
    }

    [Fact]
    public async Task ExistingWallet_UsesServerTimeForWindow()
    {
        await Earn(300);
        _clock.Advance(TimeSpan.FromSeconds(10));

        long balance = await Earn(100);

        Assert.Equal(400, balance);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, Assert.Single(_repository.Saved).LastEarnedAtUtc);
    }

    [Fact]
    public async Task ExistingWallet_TooManyCoinsTooSoon_ThrowsAndKeepsBalance()
    {
        await Earn(300);
        _clock.Advance(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<DomainException>(() => Earn(11));

        Assert.Equal(300, Assert.Single(_repository.Saved).Balance);
        Assert.Equal(1, _repository.SaveCalls);
    }

    [Fact]
    public async Task LimitComesFromSettings()
    {
        EarnCoinsHandler handler = CreateHandler(maxCoinsPerSecond: 1);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new EarnCoinsCommand(_userId, 31), CancellationToken.None));
        long balance = await handler.Handle(new EarnCoinsCommand(_userId, 30), CancellationToken.None);

        Assert.Equal(30, balance);
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }

    private sealed class InMemoryWalletRepository : IWalletRepository
    {
        private readonly List<Wallet> _added = [];

        public List<Wallet> Saved { get; } = [];
        public int SaveCalls { get; private set; }

        public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(Saved.FirstOrDefault(w => w.UserId == userId));

        public Task AddAsync(Wallet wallet, CancellationToken ct)
        {
            _added.Add(wallet);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            int count = _added.Count;
            Saved.AddRange(_added);
            _added.Clear();
            return Task.FromResult(count);
        }
    }
}
