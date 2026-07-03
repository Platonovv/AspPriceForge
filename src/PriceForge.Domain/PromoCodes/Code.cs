namespace PriceForge.Domain.PromoCodes;

public sealed record Code
{
    public string Value { get; }

    public Code(string value)
    {
        value = value.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(value) || !IsAllowedFormat(value))
        {
            throw new ArgumentException("PromoCode must be 3-64 characters, only letters, digits, dashes and underscores",
                                        nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;

    private static bool IsAllowedFormat(string value)
    {
        bool lengthCheck = value.Length is >= 3 and <= 64;
        bool charactersCheck = value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        return lengthCheck && charactersCheck;
    }
}
