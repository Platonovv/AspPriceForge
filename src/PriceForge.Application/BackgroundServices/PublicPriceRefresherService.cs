using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceForge.Application.Products;

namespace PriceForge.Application.BackgroundServices;

public class PublicPriceRefresherService : BackgroundService
{
    private readonly PublicPriceRefresherSettings _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PublicPriceRefresherService> _logger;

    private DateTime _lastSuccessfulRun = DateTime.MinValue;

    public PublicPriceRefresherService(IServiceScopeFactory serviceScopeFactory,
                                       ILogger<PublicPriceRefresherService> logger,
                                       IOptions<PublicPriceRefresherSettings> settings)
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
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.PeriodMinutes));

        _logger.LogInformation("Public price refresher started, period {PeriodMinutes} min", _settings.PeriodMinutes);

        await ExecutePriceRefresherAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecutePriceRefresherAsync(stoppingToken);
        }
    }

    private async Task ExecutePriceRefresherAsync(CancellationToken stoppingToken)
    {
        DateTime to = DateTime.UtcNow;

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            PublicPriceRefresher priceRefresher = scope.ServiceProvider.GetRequiredService<PublicPriceRefresher>();

            int updatedCount = await priceRefresher.RunOnceAsync(_lastSuccessfulRun, to, _settings.BatchSize, stoppingToken);

            _logger.LogInformation("Public price refresh updated {UpdatedCount} products", updatedCount);
            _lastSuccessfulRun = to;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Public price refresh failed, the same window will be retried on the next tick");
        }
    }
}
