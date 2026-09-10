using PriceForge.Application.TokenGenerators;

namespace PriceForge.UnitTests.GenerateTokenTests;

[Trait("Category", "Unit")]
public class TokenGeneratorTests
{
    [Fact]
    public void GenerateToken_HashMatchesCalculatedHashOfToken()
    {
        GeneratedToken generated = TokenGenerator.GenerateToken();

        Assert.Equal(TokenGenerator.CalculateHash(generated.Token), generated.Hash);
    }

    [Fact]
    public void GenerateToken_TwoCalls_ProduceDifferentTokens()
    {
        GeneratedToken first = TokenGenerator.GenerateToken();
        GeneratedToken second = TokenGenerator.GenerateToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void GenerateToken_TokenIsNotEqualToHash()
    {
        GeneratedToken generated = TokenGenerator.GenerateToken();

        Assert.NotEqual(generated.Token, generated.Hash);
    }

    [Fact]
    public void GenerateToken_ReturnsUrlSafeHexOfExpectedLength()
    {
        GeneratedToken generated = TokenGenerator.GenerateToken();

        Assert.Equal(64, generated.Token.Length);
        Assert.Equal(64, generated.Hash.Length);
        Assert.All(generated.Token, c => Assert.True(Uri.IsHexDigit(c)));
        Assert.All(generated.Hash, c => Assert.True(Uri.IsHexDigit(c)));
    }

    [Fact]
    public void CalculateHash_SameToken_ReturnsSameHash()
    {
        const string token = "ABCDEF0123456789";

        Assert.Equal(TokenGenerator.CalculateHash(token), TokenGenerator.CalculateHash(token));
    }

    [Fact]
    public void CalculateHash_DifferentTokens_ReturnDifferentHashes()
    {
        Assert.NotEqual(TokenGenerator.CalculateHash("token-one"), TokenGenerator.CalculateHash("token-two"));
    }

    [Fact]
    public void CalculateHash_IsCaseSensitive()
    {
        Assert.NotEqual(TokenGenerator.CalculateHash("abcdef"), TokenGenerator.CalculateHash("ABCDEF"));
    }
}
