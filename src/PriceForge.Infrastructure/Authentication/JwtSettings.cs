using System.ComponentModel.DataAnnotations;

namespace PriceForge.Infrastructure.Authentication;

public sealed record JwtSettings
{
    [Required]
    public string Issuer { get; init; } = string.Empty;
    [Required]
    public string Audience { get; init; } = string.Empty;
    [Required]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int AccessTokenMinutes { get; init; }
    [Range(1, int.MaxValue)]
    public int RefreshTokenDays { get; init; }
}
