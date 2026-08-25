using System.ComponentModel.DataAnnotations;

namespace PriceForge.Infrastructure.Notifications;

public sealed record SmtpSettings
{
    [Required]
    public string Host { get; init; } = "";
    [Range(1, 65535)]
    public int Port { get; init; }
    [Required]
    [EmailAddress]
    public string User { get; init; } = "";
    [Required]
    public string Password { get; init; } = "";
    [Required]
    [EmailAddress]
    public string From { get; init; } = "";
    [Required]
    [EmailAddress]
    public string To { get; init; } = "";
}
