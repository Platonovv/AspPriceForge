using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PriceForge.Application.Notifications;

namespace PriceForge.Infrastructure.Notifications;

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;

    public SmtpEmailSender(IOptions<SmtpSettings> smtpSettings)
    {
        ArgumentNullException.ThrowIfNull(smtpSettings);

        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_smtpSettings.From));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;

        var builder = new BodyBuilder {TextBody = message.Body};

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            builder.HtmlBody = message.HtmlBody;
        }

        email.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(_smtpSettings.User, _smtpSettings.Password, cancellationToken);
        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
