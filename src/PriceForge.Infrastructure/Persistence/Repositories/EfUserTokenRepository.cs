using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Authentication;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfUserTokenRepository : IUserTokenRepository
{
    private readonly AppDbContext _dbContext;

    public EfUserTokenRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<UserToken?> GetByHashAsync(string hash, UserTokenType tokenType, CancellationToken cancellationToken)
        => _dbContext.UserTokens.FirstOrDefaultAsync(x => x.Hash == hash && x.TokenType == tokenType,
                                                     cancellationToken);

    public Task<List<UserToken>> GetUserTokensByUserIdAsync(Guid userId,
                                                            UserTokenType userTokenType,
                                                            CancellationToken cancellationToken)
        => _dbContext.UserTokens
                     .Where(x => x.UserId == userId
                                 && x.TokenType == userTokenType
                                 && x.UsedAt == null
                                 && x.ExpiresAt > DateTime.UtcNow)
                     .ToListAsync(cancellationToken);

    public async Task AddAsync(UserToken userToken, CancellationToken cancellationToken)
        => await _dbContext.UserTokens.AddAsync(userToken, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
