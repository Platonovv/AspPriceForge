using Microsoft.EntityFrameworkCore;
using PriceForge.Application.PromoCodes;
using PriceForge.Domain.PromoCodes;

namespace PriceForge.Infrastructure.Persistence.Repositories;

public class EfPromoCodeRepository : IPromoCodeRepository
{
    private readonly AppDbContext _dbContext;

    public EfPromoCodeRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => _dbContext.PromoCodes.FirstOrDefaultAsync(p => p.Code == new Code(code), cancellationToken);

    public async Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken)
        => await _dbContext.PromoCodes.AddAsync(promoCode, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<bool> TryIncrementUsageAsync(PromoCode promoCode, CancellationToken cancellationToken)
    {
        int rows = await _dbContext.PromoCodes
                                   .Where(x => x.Id == promoCode.Id && x.UsedCount < x.UsedLimit)
                                   .ExecuteUpdateAsync(s => s.SetProperty(p => p.UsedCount, p => p.UsedCount + 1),
                                                       cancellationToken);
        return rows > 0;
    }
}
