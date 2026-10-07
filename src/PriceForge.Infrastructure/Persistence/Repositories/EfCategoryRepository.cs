using Microsoft.EntityFrameworkCore;
using PriceForge.Application.Categories;
using PriceForge.Domain.Categories;

namespace PriceForge.Infrastructure.Persistence.Repositories;

public class EfCategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public EfCategoryRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
        => _dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsAsync(string name, CancellationToken ct)
        => _dbContext.Categories.AnyAsync(p => p.Name == name, ct);

    public Task<bool> ExistsBindingAsync(Guid productId, Guid categoryId, CancellationToken ct)
        => _dbContext.ProductCategories.AnyAsync(p => p.ProductId == productId && p.CategoryId == categoryId, ct);

    public Task<List<Guid>> GetProductIdsAsync(Guid categoryId, CancellationToken ct)
        => _dbContext.ProductCategories.Where(x => x.CategoryId == categoryId).Select(x => x.ProductId).ToListAsync(ct);

    public async Task AddAsync(Category category, CancellationToken ct)
        => await _dbContext.Categories.AddAsync(category, ct);

    public async Task AddBindingAsync(ProductCategory productCategory, CancellationToken ct)
        => await _dbContext.ProductCategories.AddAsync(productCategory, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);
}
