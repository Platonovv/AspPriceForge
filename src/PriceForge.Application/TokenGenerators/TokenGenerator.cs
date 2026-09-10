using System.Security.Cryptography;
using System.Text;

namespace PriceForge.Application.TokenGenerators;

public static class TokenGenerator
{
    public static GeneratedToken GenerateToken()
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        return new GeneratedToken(token, CalculateHash(token));
    }

    public static string CalculateHash(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
