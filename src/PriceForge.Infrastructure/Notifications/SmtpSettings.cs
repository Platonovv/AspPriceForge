namespace PriceForge.Infrastructure.Notifications;

public sealed record SmtpSettings
{
    public string Host { get; init; } = "";
    public int Port { get; init; }
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public string From { get; init; } = "";
    public string To { get; init; } = "";
}
