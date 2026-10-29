using System.ComponentModel.DataAnnotations;

namespace PriceForge.Application.Wallets;

public sealed record WalletSettings
{
    [Range(1, 1000)]
    public int MaxCoinsPerSecond { get; init; } = 20;

    [Range(1, 300)]
    public int MaxEarnWindowSeconds { get; init; } = 30;
}
