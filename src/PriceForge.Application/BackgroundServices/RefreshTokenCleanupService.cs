using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Authentication;

namespace PriceForge.Application.BackgroundServices;

public sealed class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    private const int PeriodMinutes = 15;
    private readonly TimeSpan _grace = TimeSpan.FromDays(7);

    public RefreshTokenCleanupService(IServiceScopeFactory serviceScopeFactory,
                                      ILogger<RefreshTokenCleanupService> logger)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(PeriodMinutes));

        _logger.LogInformation("Refresh token cleanup service started at {Time}", DateTime.UtcNow);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using (IServiceScope serviceScope = _serviceScopeFactory.CreateScope())
                {
                    IRefreshTokenRepository refreshTokenRepository
                        = serviceScope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

                    int deleteCount
                        = await refreshTokenRepository.DeleteExpiredAsync(DateTime.UtcNow - _grace, stoppingToken);
                    _logger.LogInformation("Refresh token delete {DeleteCount} count", deleteCount);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Refresh token cleanup service failed at {Time}", DateTime.UtcNow);
            }
        }
    }
}
