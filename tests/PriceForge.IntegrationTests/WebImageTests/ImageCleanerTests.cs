using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Images;
using PriceForge.Domain.Common;
using PriceForge.Domain.Images;
using PriceForge.Domain.Products;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebImageTests;

[Trait("Category", "Integration")]
public class ImageCleanerTests : IClassFixture<ApiFactory>
{
    private const int MaxDeletes = 100;

    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0x11, 0x22, 0x33];

    private readonly ApiFactory _factory;

    public ImageCleanerTests(ApiFactory factory) => _factory = factory;

    private async Task<string> SeedTrackedImageAsync(string sku)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product(new Sku(sku), $"Product {sku}", new Money(1000m, Currency.RUB));
        string key = $"products/{product.Id}/{Guid.NewGuid()}.jpg";

        db.Products.Add(product);
        db.ProductImages.Add(new ProductImage(product.Id, key, "image/jpeg", JpegBytes.Length));

        await db.SaveChangesAsync();

        await _factory.PutObjectAsync(key, JpegBytes);

        return key;
    }

    private async Task<string> SeedOrphanAsync()
    {
        string key = $"products/{Guid.NewGuid()}/{Guid.NewGuid()}.jpg";

        await _factory.PutObjectAsync(key, JpegBytes);

        return key;
    }

    private async Task<(int Deleted, int Failed)> RunCleanupAsync(TimeSpan grace)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ImageCleaner cleaner = scope.ServiceProvider.GetRequiredService<ImageCleaner>();

        return await cleaner.RunOnceAsync(grace, MaxDeletes, CancellationToken.None);
    }

    [Fact]
    public async Task RunOnce_ObjectWithoutRecord_IsDeleted()
    {
        await _factory.ResetAsync();

        string orphanKey = await SeedOrphanAsync();

        (int deleted, int failed) = await RunCleanupAsync(TimeSpan.Zero);

        Assert.True(deleted >= 1);
        Assert.Equal(0, failed);
        Assert.False(await _factory.ObjectExistsAsync(orphanKey));
    }

    [Fact]
    public async Task RunOnce_ObjectWithRecord_IsKept()
    {
        await _factory.ResetAsync();

        string trackedKey = await SeedTrackedImageAsync("CLEAN-1");
        string orphanKey = await SeedOrphanAsync();

        await RunCleanupAsync(TimeSpan.Zero);

        Assert.True(await _factory.ObjectExistsAsync(trackedKey));
        Assert.False(await _factory.ObjectExistsAsync(orphanKey));
    }

    [Fact]
    public async Task RunOnce_FreshObject_IsProtectedByGrace()
    {
        await _factory.ResetAsync();

        string orphanKey = await SeedOrphanAsync();

        (int deleted, int failed) = await RunCleanupAsync(TimeSpan.FromHours(1));

        Assert.Equal(0, deleted);
        Assert.Equal(0, failed);
        Assert.True(await _factory.ObjectExistsAsync(orphanKey));
    }
}
