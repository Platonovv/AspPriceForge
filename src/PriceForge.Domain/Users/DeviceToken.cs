namespace PriceForge.Domain.Users;

public sealed class DeviceToken
{
    public Guid Id { get; }
    public string Token { get; } = null!;
    public DateTime CreatedAt { get; }

    public DeviceToken(string token, DateTime createdAt) : this(Guid.NewGuid(), token, createdAt)
    {
    }

    private DeviceToken()
    {
    }

    private DeviceToken(Guid id, string token, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be empty", nameof(token));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("CreatedAt is default", nameof(createdAt));
        }

        Id = id;
        Token = token;
        CreatedAt = createdAt;
    }
}
