namespace PriceForge.Domain.Users;

public sealed class User
{
    public Guid Id { get; }

    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;

    public User(string email, string passwordHash, string role) : this(Guid.NewGuid(), email, passwordHash, role)
    {
    }

    private User()
    {
    }

    private User(Guid id, string email, string passwordHash, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("Email must be a valid non-empty address", nameof(email));
        }

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
}
