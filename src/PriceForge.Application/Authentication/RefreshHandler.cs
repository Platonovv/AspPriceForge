using MediatR;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

public sealed class RefreshHandler : IRequestHandler<RefreshCommand, LoginResult>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshHandler(IRefreshTokenRepository refreshTokenRepository,
                          IUserRepository userRepository,
                          IJwtTokenGenerator jwtTokenGenerator)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(jwtTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResult> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        RefreshToken? tokenExists
            = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (tokenExists == null || !tokenExists.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        User? user = await _userRepository.GetUserByIdAsync(tokenExists.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        tokenExists.Revoke();

        string accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email.Value, user.Role);

        RefreshTokenResult refreshTokenResult = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new RefreshToken(refreshTokenResult.Token, user.Id, refreshTokenResult.ExpiresAt);

        await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new LoginResult(accessToken, refreshToken.Token);
    }
}
