using PriceForge.Domain.Pricing;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomerTier?> GetTierByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
