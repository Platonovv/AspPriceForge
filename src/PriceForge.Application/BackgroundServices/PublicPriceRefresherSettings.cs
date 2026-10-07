using System.ComponentModel.DataAnnotations;

namespace PriceForge.Application.BackgroundServices;

public sealed record PublicPriceRefresherSettings
{
    [Range(1, 60)]
    public int PeriodMinutes { get; init; } = 15;
    [Range(1, 999)]
    public int BatchSize { get; init; } = 500;
}
