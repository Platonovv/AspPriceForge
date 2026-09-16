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
using User = PriceForge.Domain.Users.User;

namespace PriceForge.Application.Users;

public sealed class SendConfirmEmailHandler : IRequestHandler<SendConfirmEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEmailRateLimiter _emailRateLimiter;
    private readonly EmailTokenSettings _emailTokenSettings;
    private readonly IUserTokenRepository _userTokenRepository;

    private readonly ILogger<SendConfirmEmailHandler> _logger;

    public SendConfirmEmailHandler(ILogger<SendConfirmEmailHandler> logger,
                                   IUserRepository userRepository,
                                   IUserTokenRepository userTokenRepository,
                                   IOutboxRepository outboxRepository,
                                   IOptions<EmailTokenSettings> emailTokenSettings,
                                   IEmailRateLimiter emailRateLimiter)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(emailRateLimiter);
        ArgumentNullException.ThrowIfNull(emailTokenSettings);
        ArgumentNullException.ThrowIfNull(userTokenRepository);

        _logger = logger;
        _userRepository = userRepository;
        _outboxRepository = outboxRepository;
        _emailRateLimiter = emailRateLimiter;
        _emailTokenSettings = emailTokenSettings.Value;
        _userTokenRepository = userTokenRepository;
    }

    public async Task Handle(SendConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetUserByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.IsEmailVerified)
        {
            _logger.LogInformation("User already verified");
            return;
        }

        Email userEmail = user.Email;

        if (!await _emailRateLimiter.TryConsumeAsync($"confirm-email:{userEmail.Value}",
                                                     _emailTokenSettings.EmailsPerWindowLimit,
                                                     TimeSpan.FromHours(_emailTokenSettings.WindowHour),
                                                     cancellationToken))
        {
            throw new TooManyRequestsException("Too Many Requests");
        }

        GeneratedToken token = TokenGenerator.GenerateToken();

        string confirmUrl = $"{_emailTokenSettings.FrontendBaseUrl}/confirm-email?token={token.Token}";
        var confirmEmailCreatedEvent = new SendEmailCreatedEvent(confirmUrl, user.Email.Value);
        string confirmEmailCreated = JsonSerializer.Serialize(confirmEmailCreatedEvent);
        var message = new OutboxMessage(IntegrationEventTypes.EmailConfirmationRequested, confirmEmailCreated);

        var userToken = new UserToken(request.UserId,
                                      token.Hash,
                                      UserTokenType.ConfirmedEmail,
                                      DateTime.UtcNow.AddMinutes(_emailTokenSettings.EmailConfirmationTtlMinutes));

        await _userTokenRepository.AddAsync(userToken, cancellationToken);
        await _outboxRepository.AddAsync(message, cancellationToken);
        await _userTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User send confirm email event");
    }
}
