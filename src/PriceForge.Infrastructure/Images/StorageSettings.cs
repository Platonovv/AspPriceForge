using System.ComponentModel.DataAnnotations;

namespace PriceForge.Infrastructure.Images;

public sealed record StorageSettings
{
    [Required]
    [Url]
    public string Endpoint { get; init; } = "";
    [Required]
    [Url]
    public string PublicEndpoint { get; init; } = "";
    [Required]
    public string AccessKey { get; init; } = "";
    [Required]
    public string SecretKey { get; init; } = "";
    [Required]
    public string Bucket { get; init; } = "";
    [Range(1, 1440)]
    public int DownloadUrlMinutes { get; init; } = 15;

}
