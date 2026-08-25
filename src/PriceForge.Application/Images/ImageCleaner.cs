using Microsoft.Extensions.Logging;

namespace PriceForge.Application.Images;

public sealed class ImageCleaner
{
    private readonly IImageStorage _storage;
    private readonly ILogger<ImageCleaner> _logger;
    private readonly IProductImageRepository _productImageRepository;

    public ImageCleaner(IImageStorage storage,
                        IProductImageRepository productImageRepository,
                        ILogger<ImageCleaner> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(productImageRepository);

        _logger = logger;
        _storage = storage;
        _productImageRepository = productImageRepository;
    }

    public async Task<(int deleteCount, int deleteErrorCount)> RunOnceAsync(TimeSpan grace,
                                                                            int maxDeletes,
                                                                            CancellationToken stoppingToken)
    {
        DateTime threshold = DateTime.UtcNow - grace;

        List<string> imageStorageKeys = await _storage.ListKeysAsync(threshold, stoppingToken);
        List<string> productImageKeys = await _productImageRepository.ListKeysAsync(stoppingToken);

        var deleteKeys = imageStorageKeys.Except(productImageKeys).ToList();

        if (deleteKeys.Count == 0)
        {
            return (0, 0);
        }

        if (deleteKeys.Count > maxDeletes)
        {
            _logger.LogWarning("Too many orphaned objects: {Count}, limiting to {Limit}", deleteKeys.Count, maxDeletes);
            deleteKeys = deleteKeys.Take(maxDeletes).ToList();
        }

        return await _storage.DeleteExpiredKeysAsync(deleteKeys, stoppingToken);
    }
}
