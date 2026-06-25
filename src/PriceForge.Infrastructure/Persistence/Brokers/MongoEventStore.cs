using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.Retry;
using PriceForge.Application.Brokers;
using PriceForge.Application.Products;

namespace PriceForge.Infrastructure.Persistence.Brokers;

public class MongoEventStore : IProductEventStore
{
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly IMongoCollection<ProductCreatedEvent> _collection;
    private readonly ILogger<MongoEventStore> _logger;

    private const string ProductEvents = "product_events";

    public MongoEventStore(IMongoDatabase database, ILogger<MongoEventStore> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder().AddRetry(options: new RetryStrategyOptions
                                                             {
                                                                 MaxRetryAttempts = 3,
                                                                 Delay = TimeSpan.FromMilliseconds(200),
                                                                 MaxDelay = TimeSpan.FromMilliseconds(500),
                                                                 BackoffType = DelayBackoffType.Exponential,
                                                                 UseJitter = true,
                                                                 ShouldHandle
                                                                     = new PredicateBuilder()
                                                                       .Handle<MongoException>(ex
                                                                           => ex is not MongoWriteException
                                                                           {
                                                                               WriteError.Category: ServerErrorCategory
                                                                                   .DuplicateKey
                                                                           })
                                                                       .Handle<TimeoutException>(),
                                                                 OnRetry = arguments =>
                                                                 {
                                                                     _logger.LogInformation(
                                                                         "Mongo retry number {AttemptNumber}, exception - {Ex}",
                                                                         arguments.AttemptNumber,
                                                                         arguments.Outcome.Exception);
                                                                     return default;
                                                                 }
                                                             })
                                                             .Build();

        _collection = database.GetCollection<ProductCreatedEvent>(ProductEvents);
    }

    public async Task SaveAsync(ProductCreatedEvent productEvent, CancellationToken ct)
        => await _resiliencePipeline.ExecuteAsync(
               async token => await _collection.InsertOneAsync(productEvent, null, token),
               ct);
}
