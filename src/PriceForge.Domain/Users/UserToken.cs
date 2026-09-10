namespace PriceForge.Domain.Users;

public sealed class UserToken
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string Hash { get; } = null!;
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }
    public UserTokenType TokenType { get; }
    public DateTime? UsedAt { get; private set; }

    public bool IsActive => UsedAt == null && ExpiresAt > DateTime.UtcNow;

    public UserToken(Guid userId, string hash, UserTokenType tokenType, DateTime expiresAt) : this(
        Guid.NewGuid(),
        userId,
        tokenType,
        hash,
        expiresAt)
    {
    }

    private UserToken()
    {
    }

    private UserToken(Guid id, Guid userId, UserTokenType tokenType, string hash, DateTime expiresAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        }

        if (string.IsNullOrEmpty(hash))
        {
            throw new ArgumentException("hash cannot be empty", nameof(hash));
        }

        if (expiresAt < DateTime.UtcNow)
        {
            throw new ArgumentException("Only valid expiresAt is allowed", nameof(expiresAt));
        }

        if (tokenType == UserTokenType.None)
        {
            throw new ArgumentException("Invalid token type", nameof(tokenType));
        }

        Id = id;
        Hash = hash;
        UserId = userId;
        TokenType = tokenType;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void MarkUsed() => UsedAt = DateTime.UtcNow;
}
