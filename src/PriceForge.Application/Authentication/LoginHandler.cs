using MediatR;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

internal sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginHandler(IUserRepository userRepository,
                        IRefreshTokenRepository refreshTokenRepository,
                        IPasswordHasher passwordHasher,
                        IJwtTokenGenerator jwtTokenGenerator)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(jwtTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);

        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        User? userExist = await _userRepository.GetUserByEmailAsync(new Email(request.Email), cancellationToken);

        if (userExist == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        bool isVerify = _passwordHasher.Verify(request.Password, userExist.PasswordHash);

        if (!isVerify)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        string accessToken
            = _jwtTokenGenerator.GenerateAccessToken(userExist.Id, userExist.Email.Value, userExist.Role);

        RefreshTokenResult refreshTokenResult = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new RefreshToken(refreshTokenResult.Token, userExist.Id, refreshTokenResult.ExpiresAt);

        await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new LoginResult(accessToken, refreshToken.Token);
    }
}
