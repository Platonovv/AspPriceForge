using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Authentication;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public EfRefreshTokenRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        => await _dbContext.RefreshTokens.AddAsync(token, cancellationToken);

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
        => _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public Task<int> DeleteExpiredAsync(DateTime threshold, CancellationToken cancellationToken)
        => _dbContext.RefreshTokens.Where(x => x.ExpiresAt < threshold).ExecuteDeleteAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
