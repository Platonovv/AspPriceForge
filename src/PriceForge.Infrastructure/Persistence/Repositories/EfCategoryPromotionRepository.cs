using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Carts;
using PriceForge.Application.Categories;
using PriceForge.Domain.Categories;
using PriceForge.Application.Common;

namespace PriceForge.Infrastructure.Persistence.Repositories;

public class EfCategoryPromotionRepository : ICategoryPromotionRepository
{
    private readonly AppDbContext _dbContext;

    public EfCategoryPromotionRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task AddAsync(CategoryPromotion categoryPromotion, CancellationToken ct)
        => await _dbContext.CategoryPromotions.AddAsync(categoryPromotion, ct);

    public async Task<List<CategoryPromotion>>
        GetActiveCategoryPromotionsByProductAsync(Guid productId, DateTime at, CancellationToken ct)
        => await _dbContext.ProductCategories
                           .Where(p => p.ProductId == productId)
                           .Join(_dbContext.CategoryPromotions,
                                 productCategory => productCategory.CategoryId,
                                 categoryPromotion => categoryPromotion.CategoryId,
                                 (category, promotion) => promotion)
                           .Where(x => x.StartAt <= at && at <= x.EndAt)
                           .AsNoTracking()
                           .ToListAsync(ct);

    public async Task<List<ProductCategoryPromotion>>
        GetActiveForProductsAsync(List<Guid> ids, DateTime at, CancellationToken ct)
        => await _dbContext.ProductCategories
                           .Where(pc => ids.Contains(pc.ProductId))
                           .Join(_dbContext.CategoryPromotions,
                                 pc => pc.CategoryId,
                                 cp => cp.CategoryId,
                                 (pc, cp) => new {pc.ProductId, cp})
                           .Where(x => x.cp.StartAt <= at && at <= x.cp.EndAt)
                           .Select(x => new ProductCategoryPromotion(x.ProductId, x.cp))
                           .AsNoTracking()
                           .ToListAsync(ct);

    public Task<bool> HasOverlappingAsync(Guid categoryId, DateTime startAt, DateTime endAt, CancellationToken ct)
        => _dbContext.CategoryPromotions.AnyAsync(
            x => x.CategoryId == categoryId && x.StartAt < endAt && startAt < x.EndAt,
            ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);
}
