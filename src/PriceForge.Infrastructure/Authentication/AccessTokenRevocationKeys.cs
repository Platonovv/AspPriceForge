namespace PriceForge.Infrastructure.Authentication;

public static class AccessTokenRevocationKeys
{
    public static string RevokedBefore(string userId) => $"revoked-before:user:{userId}";
}
