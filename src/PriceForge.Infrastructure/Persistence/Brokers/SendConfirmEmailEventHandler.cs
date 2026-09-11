using System.Text.Json;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;
using PriceForge.Application.Notifications;
using PriceForge.Infrastructure.Notifications;
using PriceForge.Application.Users;

namespace PriceForge.Infrastructure.Persistence.Brokers;

public sealed class SendConfirmEmailEventHandler : IIntegrationEventHandler
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SendConfirmEmailEventHandler> _logger;
    public string EventType => IntegrationEventTypes.EmailConfirmationRequested;

    public SendConfirmEmailEventHandler(IEmailSender emailSender, ILogger<SendConfirmEmailEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(emailSender);

        _logger = logger;
        _emailSender = emailSender;
    }

    public async Task HandleAsync(string message, CancellationToken ct)
    {
        SendEmailCreatedEvent sendEmailCreatedEvent = JsonSerializer.Deserialize<SendEmailCreatedEvent>(message)
                                                      ?? throw new InvalidOperationException(
                                                          "Failed to deserialize SendEmailCreatedEvent");

        var email = new EmailMessage(sendEmailCreatedEvent.Email,
                                     "Подтвердите адрес в PriceForge",
                                     EmailTemplates.ConfirmEmailText(sendEmailCreatedEvent.ConfirmUrl),
                                     EmailTemplates.ConfirmEmailHtml(sendEmailCreatedEvent.ConfirmUrl));

        await _emailSender.SendAsync(email, ct);

        _logger.LogInformation("Подтверждение почты {Email}", sendEmailCreatedEvent.Email);
    }
}
