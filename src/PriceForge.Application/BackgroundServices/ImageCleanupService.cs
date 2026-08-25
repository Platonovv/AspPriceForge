using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceForge.Application.Images;

namespace PriceForge.Application.BackgroundServices;

public sealed class ImageCleanupService : BackgroundService
{
    private readonly ImageCleanupSettings _settings;
    private readonly ILogger<ImageCleanupService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ImageCleanupService(IServiceScopeFactory serviceScopeFactory,
                               ILogger<ImageCleanupService> logger,
                               IOptions<ImageCleanupSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);

        _logger = logger;
        _settings = settings.Value;

        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.PeriodMinutes));

        _logger.LogInformation("Starting image cleanup service {Time}", DateTime.UtcNow);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    ImageCleaner imageCleaner = scope.ServiceProvider.GetRequiredService<ImageCleaner>();
                    (int deleteCount, int deleteErrorCount)
                        = await imageCleaner.RunOnceAsync(_settings.Grace, _settings.MaxDeletesPerRun, stoppingToken);

                    _logger.LogInformation("Image cleanup delete {DeleteCount}, failed delete {DeleteErrorsCount}",
                                           deleteCount,
                                           deleteErrorCount);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Image cleanup service failed at {Time}", DateTime.UtcNow);
            }
        }
    }
}
