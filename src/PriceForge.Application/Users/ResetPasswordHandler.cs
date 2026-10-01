using MediatR;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Authentication;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.TokenGenerators;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Users;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenRevoker _accessTokenRevoker;
    private readonly ILogger<ResetPasswordHandler> _logger;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ResetPasswordHandler(ILogger<ResetPasswordHandler> logger,
                                IUserRepository userRepository,
                                IUserTokenRepository userTokenRepository,
                                IPasswordHasher passwordHasher,
                                IRefreshTokenRepository refreshTokenRepository,
                                IAccessTokenRevoker accessTokenRevoker)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(accessTokenRevoker);
        ArgumentNullException.ThrowIfNull(userTokenRepository);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);

        _logger = logger;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenRevoker = accessTokenRevoker;
        _userTokenRepository = userTokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        string hash = TokenGenerator.CalculateHash(request.Token);

        UserToken? userToken
            = await _userTokenRepository.GetByHashAsync(hash, UserTokenType.ChangePassword, cancellationToken);

        if (userToken == null || !userToken.IsActive)
        {
            throw new BusinessRuleException("The link is invalid or expired; please request the email again.");
        }

        User? user = await _userRepository.GetUserByIdAsync(userToken.UserId, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleException("The link is invalid or expired; please request the email again.");
        }

        string hashPassword = _passwordHasher.Hash(request.NewPassword);

        List<UserToken> userTokens
            = await _userTokenRepository.GetUserTokensByUserIdAsync(user.Id,
                                                                    UserTokenType.ChangePassword,
                                                                    cancellationToken);

        List<RefreshToken> tokens = await _refreshTokenRepository.GetTokensByUserIdAsync(user.Id, cancellationToken);

        user.PasswordReset(hashPassword);

        foreach (UserToken token in userTokens)
        {
            token.MarkUsed();
        }

        foreach (RefreshToken token in tokens)
        {
            token.Revoke();
        }

        await _accessTokenRevoker.RevokeAllAsync(user.Id, cancellationToken);

        await _userTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
