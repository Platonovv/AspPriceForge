namespace PriceForge.Application.Cache;

public interface ICacheVersion
{
    Task<string> GetAsync(CancellationToken ct);
    Task BumpAsync(CancellationToken ct);
}
