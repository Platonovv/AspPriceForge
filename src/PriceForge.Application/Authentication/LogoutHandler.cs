using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, LogoutResult>
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<LogoutHandler> _logger;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutHandler(ILogger<LogoutHandler> logger,
                         IRefreshTokenRepository refreshTokenRepository,
                         IDistributedCache distributedCache)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(distributedCache);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _distributedCache = distributedCache;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _distributedCache.SetStringAsync($"revoked:jti:{request.Jti}",
                                               $"{request.Jti}",
                                               new DistributedCacheEntryOptions
                                               {
                                                   AbsoluteExpiration = request.ExpiresAt
                                               },
                                               cancellationToken);

        _logger.LogInformation("Revoked {Jti} from Distributed Cache", request.Jti);

        List<RefreshToken> tokens
            = await _refreshTokenRepository.GetTokensByUserIdAsync(request.UserId, cancellationToken);

        foreach (RefreshToken token in tokens)
        {
            token.Revoke();
        }

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new LogoutResult();
    }
}
