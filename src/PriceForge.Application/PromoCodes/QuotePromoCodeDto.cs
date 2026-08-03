using PriceForge.Domain.Common;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Application.PromoCodes;

public record QuotePromoCodeDto(decimal TotalPrice, Currency Currency, PromoCodeReason  PromoCodeReason);
