using PriceForge.Domain.Categories;

namespace PriceForge.Application.Categories;

public interface ICategoryRepository
{
    // Чтение
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsAsync(string name, CancellationToken ct);
    Task<bool> ExistsBindingAsync(Guid productId, Guid categoryId, CancellationToken ct);

    // Запись
    Task AddAsync(Category category, CancellationToken ct);
    Task AddBindingAsync(ProductCategory productCategory, CancellationToken ct);

    // Сохранение всех накопленных изменений (UoW commit)
    Task<int> SaveChangesAsync(CancellationToken ct);
}
