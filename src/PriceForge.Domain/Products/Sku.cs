namespace PriceForge.Domain.Products;

public sealed record Sku
{
    public string Value { get; }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsAllowedFormat(value))
        {
            throw new ArgumentException("SKU must be 3-64 characters, only letters, digits, dashes and underscores",
                                        nameof(value));
        }

        Value = value;
    }

    public override string ToString() => Value;

    public static bool TryCreate(string? value, out Sku? sku)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsAllowedFormat(value))
        {
            sku = null;
            return false;
        }

        sku = new Sku(value);
        return true;
    }

    private static bool IsAllowedFormat(string value)
    {
        bool lengthCheck = value.Length is >= 3 and <= 64;
        bool charactersCheck = value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        return lengthCheck && charactersCheck;
    }
}
