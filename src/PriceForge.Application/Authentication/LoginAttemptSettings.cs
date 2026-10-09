using System.ComponentModel.DataAnnotations;

namespace PriceForge.Application.Authentication;

public sealed record LoginAttemptSettings
{
    [Range(1, 10)]
    public int MaxFailedAttempts { get; init; } = 5;

    [Range(1, 60)]
    public int WindowMinutes { get; init; } = 15;
}
