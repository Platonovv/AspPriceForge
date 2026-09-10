using System.ComponentModel.DataAnnotations;

namespace PriceForge.Application.Users.UserTokens;

public sealed record EmailTokenSettings
{
    [Required]
    [Url]
    public string FrontendBaseUrl { get; init; } = "";

    [Range(1, 10080)]
    public int EmailConfirmationTtlMinutes { get; init; } = 1440;

    [Range(1, 1440)]
    public int PasswordResetTtlMinutes { get; init; } = 30;

    [Range(1, 100)]
    public int EmailsPerWindowLimit { get; init; } = 3;

    [Range(1, 100)]
    public int WindowHour { get; init; } = 1;
}
