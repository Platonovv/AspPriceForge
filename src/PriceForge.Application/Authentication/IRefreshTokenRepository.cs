using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

public interface IRefreshTokenRepository
{
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<int> DeleteExpiredAsync(DateTime threshold, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
