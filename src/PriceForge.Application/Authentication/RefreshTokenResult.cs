namespace PriceForge.Application.Authentication;

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAt);
