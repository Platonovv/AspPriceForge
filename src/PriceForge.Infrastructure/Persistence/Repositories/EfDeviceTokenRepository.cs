using Microsoft.EntityFrameworkCore;
using PriceForge.Application.DeviceTokens;
using PriceForge.Domain.Users;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfDeviceTokenRepository : IDeviceTokenRepository
{
    private readonly AppDbContext _dbContext;

    public EfDeviceTokenRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task AddAsync(DeviceToken token, CancellationToken ct)
        => await _dbContext.DeviceTokens.AddAsync(token, ct);

    public async Task<IReadOnlyList<DeviceTokenDto>> GetAllTokensAsync(CancellationToken ct)
        => await _dbContext.DeviceTokens.Select(x => new DeviceTokenDto(x.Id, x.Token)).ToListAsync(ct);

    public Task<DeviceTokenDto?> GetSummaryTokenAsync(string token, CancellationToken ct)
        => _dbContext.DeviceTokens
                     .Where(d => d.Token == token)
                     .Select(d => new DeviceTokenDto(d.Id, token))
                     .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(string token, CancellationToken ct)
        => _dbContext.DeviceTokens.AnyAsync(p => p.Token == token, ct);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
