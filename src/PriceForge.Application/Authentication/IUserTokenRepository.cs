using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

public interface IUserTokenRepository
{
    Task<UserToken?> GetByHashAsync(string hash, UserTokenType tokenType, CancellationToken cancellationToken);

    Task<List<UserToken>> GetUserTokensByUserIdAsync(Guid userId,
                                                     UserTokenType userTokenType,
                                                     CancellationToken cancellationToken);

    Task AddAsync(UserToken userToken, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
