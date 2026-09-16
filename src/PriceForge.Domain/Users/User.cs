using PriceForge.Domain.Pricing;

namespace PriceForge.Domain.Users;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public CustomerTier Tier { get; private set; } = CustomerTier.Regular;
    public bool IsEmailVerified { get; private set; } = false;

    public User(Email email, string passwordHash, string role) : this(Guid.NewGuid(), email, passwordHash, role)
    {
    }

    private User()
    {
    }

    private User(Guid id, Email email, string passwordHash, string role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash cannot be null or whitespace", nameof(passwordHash));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));
        }

        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void ChangeTier(CustomerTier newTier)
    {
        if (newTier == CustomerTier.None || !Enum.IsDefined(typeof(CustomerTier), newTier))
        {
            throw new ArgumentException("Tier must be defined", nameof(newTier));
        }

        Tier = newTier;
    }

    public void EmailConfirmation() => IsEmailVerified = true;

    public void PasswordReset(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("PasswordHash cannot be null or whitespace", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
    }
}
