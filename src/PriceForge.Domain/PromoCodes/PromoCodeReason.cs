namespace PriceForge.Domain.PromoCodes;

public enum PromoCodeReason
{
    None = 0,
    IsTimeNotValid = 1,
    IsOrderNotValid = 2,
    IsUsedCountNotValid = 3,
    IsPromoCodeNotFound = 4,
}
