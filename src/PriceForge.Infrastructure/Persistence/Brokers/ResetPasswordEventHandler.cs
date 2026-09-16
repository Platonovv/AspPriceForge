using System.Text.Json;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using PriceForge.Application.Notifications;
using PriceForge.Application.Users;
using PriceForge.Infrastructure.Notifications;

namespace PriceForge.Infrastructure.Persistence.Brokers;

public sealed class ResetPasswordEventHandler : IIntegrationEventHandler
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ResetPasswordEventHandler> _logger;

    public string EventType => IntegrationEventTypes.PasswordResetRequested;

    public ResetPasswordEventHandler(IEmailSender emailSender, ILogger<ResetPasswordEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(emailSender);

        _logger = logger;
        _emailSender = emailSender;
    }

    public async Task HandleAsync(string message, CancellationToken ct)
    {
        ResetPasswordEvent resetPasswordEvent = JsonSerializer.Deserialize<ResetPasswordEvent>(message)
                                                ?? throw new JsonException("Failed to deserialize ResetPasswordEvent");

        var email = new EmailMessage(resetPasswordEvent.Email,
                                     "Сброс пароля",
                                     EmailTemplates.ResetPasswordText(resetPasswordEvent.ResetUrl),
                                     EmailTemplates.ResetPasswordHtml(resetPasswordEvent.ResetUrl));

        await _emailSender.SendAsync(email, ct);

        _logger.LogInformation("Сброс пароля {Email}", resetPasswordEvent.Email);
    }
}
