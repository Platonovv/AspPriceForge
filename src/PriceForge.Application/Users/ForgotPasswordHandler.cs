using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceForge.Application.Authentication;
using PriceForge.Application.Brokers;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Notifications;
using PriceForge.Application.TokenGenerators;
using PriceForge.Application.Users.UserTokens;
using PriceForge.Domain.Users;

namespace PriceForge.Application.Users;

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailRateLimiter _emailRateLimiter;
    private readonly IOutboxRepository _outboxRepository;
    private readonly EmailTokenSettings _emailTokenSettings;
    private readonly ILogger<ForgotPasswordHandler> _logger;
    private readonly IUserTokenRepository _userTokenRepository;

    public ForgotPasswordHandler(ILogger<ForgotPasswordHandler> logger,
                                 IUserRepository userRepository,
                                 IEmailRateLimiter emailRateLimiter,
                                 IOptions<EmailTokenSettings> emailTokenSettings,
                                 IUserTokenRepository userTokenRepository,
                                 IOutboxRepository outboxRepository)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(emailRateLimiter);
        ArgumentNullException.ThrowIfNull(userTokenRepository);

        _logger = logger;
        _userRepository = userRepository;
        _emailRateLimiter = emailRateLimiter;
        _outboxRepository = outboxRepository;
        _userTokenRepository = userTokenRepository;
        _emailTokenSettings = emailTokenSettings.Value;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        string userEmail = request.Email.ToLowerInvariant();

        if (!await _emailRateLimiter.TryConsumeAsync($"reset-password:{userEmail}",
                                                     _emailTokenSettings.EmailsPerWindowLimit,
                                                     TimeSpan.FromHours(_emailTokenSettings.WindowHour),
                                                     cancellationToken))
        {
            throw new TooManyRequestsException("Too Many Requests");
        }

        User? user = await _userRepository.GetUserByEmailAsync(userEmail, cancellationToken);

        if (user == null)
        {
            return;
        }

        GeneratedToken token = TokenGenerator.GenerateToken();

        string resetPasswordUrl = $"{_emailTokenSettings.FrontendBaseUrl}/reset-password?token={token.Token}";
        var resetPasswordEvent = new ResetPasswordEvent(resetPasswordUrl, user.Email);
        string resetPassword = JsonSerializer.Serialize(resetPasswordEvent);
        var message = new OutboxMessage(IntegrationEventTypes.PasswordResetRequested, resetPassword);

        var userToken = new UserToken(user.Id,
                                      token.Hash,
                                      UserTokenType.ChangePassword,
                                      DateTime.UtcNow.AddMinutes(_emailTokenSettings.PasswordResetTtlMinutes));

        await _userTokenRepository.AddAsync(userToken, cancellationToken);
        await _outboxRepository.AddAsync(message, cancellationToken);
        await _userTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User send reset password event");
    }
}
