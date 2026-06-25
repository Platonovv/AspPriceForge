using PriceForge.Domain.Users;

namespace PriceForge.Application.DeviceTokens;

public interface IDeviceTokenRepository
{
    Task AddAsync(DeviceToken token, CancellationToken ct);
    Task<IReadOnlyList<DeviceTokenDto>> GetAllTokensAsync(CancellationToken ct);
    Task<DeviceTokenDto?> GetSummaryTokenAsync(string token, CancellationToken ct);
    Task<bool> ExistsAsync(string token, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
