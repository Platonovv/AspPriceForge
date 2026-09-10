using Microsoft.Extensions.Options;
using PriceForge.Application.Notifications;

namespace PriceForge.Infrastructure.Notifications;

public sealed class EmailChannel : INotificationChannel
{
    private readonly SmtpSettings _smtpSettings;
    private readonly IEmailSender _emailSender;

    public EmailChannel(IOptions<SmtpSettings> smtpSettings, IEmailSender emailSender)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(smtpSettings);

        _emailSender = emailSender;
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendAsync(string subject, string message, CancellationToken ct)
        => await _emailSender.SendAsync(new EmailMessage(_smtpSettings.To, subject, message), ct);
}
