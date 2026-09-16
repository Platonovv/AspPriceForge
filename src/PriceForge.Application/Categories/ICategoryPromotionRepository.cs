using PriceForge.Application.Carts;
using PriceForge.Domain.Categories;
using PriceForge.Application.Common;

namespace PriceForge.Application.Categories;

public interface ICategoryPromotionRepository
{
    Task AddAsync(CategoryPromotion categoryPromotion, CancellationToken ct);

    Task<List<CategoryPromotion>> GetActiveCategoryPromotionsByProductAsync(Guid productId,
                                                                            DateTime currentTime,
                                                                            CancellationToken ct);

    Task<List<ProductCategoryPromotion>> GetActiveForProductsAsync(List<Guid> ids, DateTime at, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<bool> HasOverlappingAsync(Guid categoryId, DateTime startAt, DateTime endAt, CancellationToken ct);
}
