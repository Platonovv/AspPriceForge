using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceForge.Application.Brokers;

namespace PriceForge.Application.BackgroundServices;

public class OutboxDispatcher : BackgroundService
{
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private const int PeriodSeconds = 15;

    public OutboxDispatcher(IServiceScopeFactory serviceScopeFactory, ILogger<OutboxDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(PeriodSeconds));

        _logger.LogInformation("Outbox Dispatcher started at {Time}", DateTime.UtcNow);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using (IServiceScope scope = _serviceScopeFactory.CreateScope())
                {
                    IOutboxRepository outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    IEventPublisher eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                    List<OutboxMessage> unpublishedList = await outboxRepository.GetUnpublishedAsync(stoppingToken);

                    foreach (OutboxMessage outboxMessage in unpublishedList)
                    {
                        await eventPublisher.PublishAsync(outboxMessage, stoppingToken);
                        outboxMessage.MarkPublished();
                    }

                    await outboxRepository.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Outbox Dispatcher failed at {Time}", DateTime.UtcNow);
            }
        }
    }
}
