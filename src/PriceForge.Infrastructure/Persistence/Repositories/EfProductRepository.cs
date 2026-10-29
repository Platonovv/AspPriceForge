using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Products;
using PriceForge.Application.Search;
using PriceForge.Domain.Products;

namespace PriceForge.Infrastructure.Persistence.Repositories;

internal sealed class EfProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public EfProductRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        => _dbContext.Products.Include(x => x.Components).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
        => _dbContext.Products.Include(x => x.Components).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken ct)
        => _dbContext.Products.Include(x => x.Components).AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);

    public Task<List<Product>> GetByIdsForUpdateAsync(List<Guid> ids, CancellationToken ct)
        => _dbContext.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct)
        => _dbContext.Products.Where(p => ids.Contains(p.Id)).AsNoTracking().ToListAsync(ct);

    public async Task<(IReadOnlyList<Product> Items, int Total)> GetProductsByFilterAsync(
        string? searchString,
        ProductSearchFilter filter,
        CancellationToken ct)
    {
        IQueryable<Product> query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            bool tryCreate = Sku.TryCreate(searchString, out Sku? sku);
            query = tryCreate
                        ? query.Where(p => EF.Functions.ILike(p.Name, $"%{searchString}%") || p.Sku == sku)
                        : query.Where(p => EF.Functions.ILike(p.Name, $"%{searchString}%"));
        }

        if (filter.Category != null)
        {
            query = query.Where(p => _dbContext.ProductCategories.Any(pc => pc.ProductId == p.Id
                                                                            && pc.CategoryId == filter.Category));
        }

        if (filter.MaxPrice is > 0)
        {
            query = query.Where(p => p.PublicPrice <= filter.MaxPrice);
        }

        query = filter.Descending
                    ? query.OrderByDescending(p => p.PublicPrice).ThenBy(p => p.Id)
                    : query.OrderBy(p => p.PublicPrice).ThenBy(p => p.Id);

        int total = await query.CountAsync(ct);

        query = query.Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize);

        return (await query.ToListAsync(ct), total);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => _dbContext.Products.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(Product product, CancellationToken ct)
        => await _dbContext.Products.AddAsync(product, ct);

    public void Remove(Product product) => _dbContext.Products.Remove(product);
    public void ClearTracking() => _dbContext.ChangeTracker.Clear();

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           })
        {
            throw new ConflictException("Product already exists");
        }
    }
}
