using Microsoft.EntityFrameworkCore;
using Npgsql;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Products;
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

    public Task<ProductSummaryDto?> GetSummaryAsync(Guid id, CancellationToken ct)
        => _dbContext.Products
                     .Where(p => p.Id == id)
                     .Select(p => new ProductSummaryDto(p.Name, p.BasePrice.Amount))
                     .FirstOrDefaultAsync(ct);

    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken ct)
        => _dbContext.Products.Include(x => x.Components).AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);



    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => _dbContext.Products.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(Product product, CancellationToken ct)
        => await _dbContext.Products.AddAsync(product, ct);

    public void Remove(Product product) => _dbContext.Products.Remove(product);

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
