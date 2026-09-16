namespace PriceForge.Domain.Users;

public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        string formatValue = value.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(formatValue) || !IsAllowedFormat(formatValue))
        {
            throw new ArgumentException("Email must contain @ and be 3-256 characters", nameof(value));
        }

        Value = formatValue;
    }

    private static bool IsAllowedFormat(string value)
    {
        bool lengthCheck = value.Length is >= 3 and <= 256;
        bool emailCheck = value.Contains('@');
        return lengthCheck && emailCheck;
    }
}
