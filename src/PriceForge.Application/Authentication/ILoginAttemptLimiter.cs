namespace PriceForge.Application.Authentication;

public interface ILoginAttemptLimiter
{
    Task<bool> IsBlockedAsync(string key, CancellationToken cancellationToken);
    Task RegisterFailureAsync(string key, CancellationToken cancellationToken);
    Task ResetAsync(string key, CancellationToken cancellationToken);
}
