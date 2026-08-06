using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Authentication;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Domain.Pricing;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public EfUserRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken: cancellationToken);

    public Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CustomerTier?> GetTierByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Users
                     .Where(x => x.Id == id)
                     .AsNoTracking()
                     .Select(x => (CustomerTier?) x.Tier)
                     .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
        => await _dbContext.Users.AddAsync(user, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
