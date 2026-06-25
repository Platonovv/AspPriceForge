using PriceForge.Application.DeviceTokens;
using PriceForge.Application.Notifications;
using FirebaseAdmin.Messaging;

namespace PriceForge.Infrastructure.Notifications;

public class PushChannel : INotificationChannel
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;

    public PushChannel(IDeviceTokenRepository deviceTokenRepository)
    {
        ArgumentNullException.ThrowIfNull(deviceTokenRepository);
        _deviceTokenRepository = deviceTokenRepository;
    }

    public async Task SendAsync(string subject, string message, CancellationToken ct)
    {
        var tokens = (await _deviceTokenRepository.GetAllTokensAsync(ct)).Select(d => d.Token).ToList();

        if (tokens.Count == 0)
        {
            return;
        }

        var fcmMessage = new MulticastMessage
        {
            Tokens = tokens, Notification = new Notification {Title = subject, Body = message},
        };

        await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(fcmMessage, ct);
    }
}
