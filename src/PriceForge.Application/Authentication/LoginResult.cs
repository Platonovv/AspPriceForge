namespace PriceForge.Application.Authentication;

public sealed record LoginResult(string AccessToken, string RefreshToken);
