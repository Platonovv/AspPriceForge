namespace PriceForge.Application.Notifications;

public interface INotificationChannel
{
    Task SendAsync(string subject, string message, CancellationToken ct);
}
