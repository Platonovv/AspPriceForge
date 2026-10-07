namespace PriceForge.Domain.Pricing;

public static class PriceRounding
{
    public static decimal RoundToCents(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
