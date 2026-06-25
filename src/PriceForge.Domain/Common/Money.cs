namespace PriceForge.Domain.Common;

public sealed record Money
{
    public decimal Amount { get; }

    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative");
        }

        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
