using PriceForge.Domain.PromoCodes;

namespace PriceForge.Application.PromoCodes;

public interface IPromoCodeRepository
{
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> TryIncrementUsageAsync(PromoCode promoCode, CancellationToken cancellationToken);
}
