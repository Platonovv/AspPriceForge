namespace PriceForge.Domain.PromoCodes;

public sealed class PromoCode
{
    public Guid Id { get; }

    public Code Code { get; }

    public decimal DiscountPercent { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }

    public int UsedCount { get; private set; }

    public int UsedLimit { get; }

    public decimal MinOrderAmount { get; }

    public PromoCode(Code code,
                     decimal discountPercent,
                     DateTime validFrom,
                     DateTime validTo,
                     int usedLimit,
                     decimal minOrderAmount) : this(Guid.NewGuid(),
                                                    code,
                                                    discountPercent,
                                                    validFrom,
                                                    validTo,
                                                    usedLimit,
                                                    minOrderAmount)
    {
    }

    private PromoCode()
    {
    }

    private PromoCode(Guid id,
                      Code code,
                      decimal discountPercent,
                      DateTime validFrom,
                      DateTime validTo,
                      int usedLimit,
                      decimal minOrderAmount)
    {
        if (discountPercent is > 100 or <= 0)
        {
            throw new ArgumentException("PromoCode percent must be in range (0, 100]", nameof(discountPercent));
        }

        if (validFrom == default)
        {
            throw new ArgumentException("PromoCode validFrom must be set", nameof(validFrom));
        }

        if (validTo == default)
        {
            throw new ArgumentException("PromoCode validTo must be set", nameof(validTo));
        }

        if (validFrom > validTo)
        {
            throw new ArgumentException("PromoCode validFrom must be less than validTo", nameof(validFrom));
        }

        if (usedLimit <= 0)
        {
            throw new ArgumentException("PromoCode usedLimit must be > 0", nameof(usedLimit));
        }

        if (minOrderAmount < 0)
        {
            throw new ArgumentException("PromoCode minOrderAmount must be >= 0", nameof(minOrderAmount));
        }

        Id = id;
        Code = code;
        DiscountPercent = discountPercent;
        ValidFrom = validFrom;
        ValidTo = validTo;
        UsedLimit = usedLimit;
        MinOrderAmount = minOrderAmount;
    }

    public bool IsValid(decimal orderAmount, DateTime now)
    {
        bool isTimeValid = now >= ValidFrom && now <= ValidTo;
        bool isOrderValid = orderAmount >= MinOrderAmount;
        bool isUsedCountValid = UsedCount < UsedLimit;
        return isTimeValid && isOrderValid && isUsedCountValid;
    }

    public void IncrementUsage()
    {
        if (UsedCount < UsedLimit)
        {
            UsedCount++;
        }
    }
}
