namespace PriceForge.Infrastructure.Authentication;

public sealed record JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; }
    public int RefreshTokenDays { get; init; }
}
