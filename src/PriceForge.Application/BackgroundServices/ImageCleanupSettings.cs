using System.ComponentModel.DataAnnotations;

namespace PriceForge.Application.BackgroundServices;

public sealed record ImageCleanupSettings
{
    [Range(1, 60)]
    public int PeriodMinutes { get; init; } = 15;
    [Range(1, 999)]
    public int MaxDeletesPerRun { get; init; } = 500;

    public TimeSpan Grace { get; init; } = TimeSpan.FromHours(1);
}
