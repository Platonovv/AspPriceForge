using MediatR;
using PriceForge.Domain.Users;

namespace PriceForge.Application.DeviceTokens;

public sealed class CreateDeviceTokenHandler : IRequestHandler<CreateDeviceTokenCommand>
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;

    public CreateDeviceTokenHandler(IDeviceTokenRepository deviceTokenRepository)
    {
        ArgumentNullException.ThrowIfNull(deviceTokenRepository);
        _deviceTokenRepository = deviceTokenRepository;
    }

    public async Task Handle(CreateDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        bool existToken = await _deviceTokenRepository.ExistsAsync(request.Token, cancellationToken);

        if (existToken)
        {
            return;
        }

        var token = new DeviceToken(request.Token, DateTime.UtcNow);
        await _deviceTokenRepository.AddAsync(token, cancellationToken);
        await _deviceTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
