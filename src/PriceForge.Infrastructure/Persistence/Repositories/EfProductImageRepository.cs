using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Images;
using PriceForge.Domain.Images;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfProductImageRepository : IProductImageRepository
{
    private readonly AppDbContext _dbContext;

    public EfProductImageRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<ProductImage?> GetByProductIdAsync(Guid productId, CancellationToken ct)
        => _dbContext.ProductImages.FirstOrDefaultAsync(p => p.ProductId == productId, ct);

    public Task<List<string>> ListKeysAsync(CancellationToken stoppingToken)
        => _dbContext.ProductImages.Select(x => x.Key).AsNoTracking().ToListAsync(stoppingToken);

    public async Task AddAsync(ProductImage productImage, CancellationToken ct)
        => await _dbContext.ProductImages.AddAsync(productImage, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);
}
