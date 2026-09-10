namespace PriceForge.Application.Notifications;

public interface IEmailRateLimiter
{
    Task<bool> TryConsumeAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken);
}
