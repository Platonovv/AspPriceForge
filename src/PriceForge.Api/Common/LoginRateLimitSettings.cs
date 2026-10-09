using System.ComponentModel.DataAnnotations;

namespace PriceForge.Api.Common;

public sealed record LoginRateLimitSettings
{
    [Range(1, 60)]
    public int PermitLimit { get; init; } = 20;

    [Range(1, 60)]
    public int WindowMinutes { get; init; } = 5;
}
