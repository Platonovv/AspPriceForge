using Microsoft.Extensions.Logging;
using PriceForge.Application.Cache;
using PriceForge.Application.Categories;
using PriceForge.Application.Promotions;

namespace PriceForge.Application.Products;

public sealed class PublicPriceRefresher
{
    private readonly ICacheVersion _cacheVersion;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<PublicPriceRefresher> _logger;
    private readonly IPromotionRepository _promotionRepository;
    private readonly PublicPriceRecalculator _publicPriceRecalculator;
    private readonly ICategoryPromotionRepository _categoryPromotionRepository;

    public PublicPriceRefresher(ILogger<PublicPriceRefresher> logger,
                                IPromotionRepository promotionRepository,
                                ICategoryPromotionRepository categoryPromotionRepository,
                                IProductRepository productRepository,
                                PublicPriceRecalculator publicPriceRecalculator,
                                ICacheVersion cacheVersion)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cacheVersion);
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(promotionRepository);
        ArgumentNullException.ThrowIfNull(publicPriceRecalculator);
        ArgumentNullException.ThrowIfNull(categoryPromotionRepository);

        _logger = logger;
        _cacheVersion = cacheVersion;
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
        _publicPriceRecalculator = publicPriceRecalculator;
        _categoryPromotionRepository = categoryPromotionRepository;
    }

    public async Task<int> RunOnceAsync(DateTime from, DateTime to, int batchSize, CancellationToken stoppingToken)
    {
        List<Guid> promotionProducts
            = await _promotionRepository.GetProductIdsWithBoundaryInWindowAsync(from, to, stoppingToken);

        List<Guid> categoryPromotionProducts
            = await _categoryPromotionRepository.GetProductIdsWithBoundaryInWindowAsync(from, to, stoppingToken);

        var productsIds = promotionProducts.Union(categoryPromotionProducts).ToList();

        if (productsIds.Count == 0)
        {
            return 0;
        }

        IEnumerable<Guid[]> chunkIds = productsIds.Chunk(batchSize);

        int updateCounter = 0;

        foreach (Guid[] guids in chunkIds)
        {
            await _publicPriceRecalculator.RecalculateAsync(guids.ToList(), stoppingToken);
            updateCounter += await _productRepository.SaveChangesAsync(stoppingToken);
            _productRepository.ClearTracking();
        }

        if (updateCounter == 0)
        {
            return 0;
        }

        await _cacheVersion.BumpAsync(stoppingToken);

        return updateCounter;
    }
}
