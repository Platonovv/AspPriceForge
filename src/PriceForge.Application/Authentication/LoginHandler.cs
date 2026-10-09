using MediatR;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Notifications;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Authentication;

internal sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILoginAttemptLimiter _loginAttemptLimiter;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginHandler(IUserRepository userRepository,
                        IRefreshTokenRepository refreshTokenRepository,
                        IPasswordHasher passwordHasher,
                        IJwtTokenGenerator jwtTokenGenerator,
                        ILoginAttemptLimiter loginAttemptLimiter)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(jwtTokenGenerator);
        ArgumentNullException.ThrowIfNull(loginAttemptLimiter);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);

        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _loginAttemptLimiter = loginAttemptLimiter;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);

        if (await _loginAttemptLimiter.IsBlockedAsync(email.Value, cancellationToken))
        {
            throw new TooManyRequestsException("Too many requests");
        }

        User? userExist = await _userRepository.GetUserByEmailAsync(email, cancellationToken);

        if (userExist == null)
        {
            await _loginAttemptLimiter.RegisterFailureAsync(email.Value, cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        bool isVerify = _passwordHasher.Verify(request.Password, userExist.PasswordHash);

        if (!isVerify)
        {
            await _loginAttemptLimiter.RegisterFailureAsync(email.Value, cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        string accessToken
            = _jwtTokenGenerator.GenerateAccessToken(userExist.Id, userExist.Email.Value, userExist.Role);

        RefreshTokenResult refreshTokenResult = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new RefreshToken(refreshTokenResult.Token, userExist.Id, refreshTokenResult.ExpiresAt);

        await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        await _loginAttemptLimiter.ResetAsync(email.Value, cancellationToken);

        return new LoginResult(accessToken, refreshToken.Token);
    }
}
