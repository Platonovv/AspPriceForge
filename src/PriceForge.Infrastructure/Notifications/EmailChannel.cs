using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PriceForge.Application.Notifications;

namespace PriceForge.Infrastructure.Notifications;

public sealed class EmailChannel : INotificationChannel
{
    private readonly SmtpSettings _smtpSettings;

    public EmailChannel(IOptions<SmtpSettings> smtpSettings)
    {
        ArgumentNullException.ThrowIfNull(smtpSettings);

        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendAsync(string subject, string message, CancellationToken ct)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_smtpSettings.From));
        email.To.Add(MailboxAddress.Parse(_smtpSettings.To));
        email.Subject = subject;
        email.Body = new TextPart("plain") {Text = message};

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.Auto, ct);
            await client.AuthenticateAsync(_smtpSettings.User, _smtpSettings.Password, ct);
            await client.SendAsync(email, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
