namespace PriceForge.Domain.Users;

public sealed class RefreshToken
{
    public Guid Id { get; }
    public string Token { get; } = null!;
    public Guid UserId { get; }
    public DateTime ExpiresAt { get; }

    public bool IsRevoked { get; private set; }

    public RefreshToken(string token, Guid userId, DateTime expiresAt) : this(Guid.NewGuid(), token, userId, expiresAt)
    {
    }

    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, string token, Guid userId, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be empty", nameof(token));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Only valid userId are allowed", nameof(userId));
        }

        if (expiresAt < DateTime.UtcNow)
        {
            throw new ArgumentException("Only valid expiresAt is allowed", nameof(expiresAt));
        }

        Id = id;
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
    }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke() => IsRevoked = true;
}
