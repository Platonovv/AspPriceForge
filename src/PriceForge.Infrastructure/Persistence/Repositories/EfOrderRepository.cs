using PriceForge.Application.Orders;
using PriceForge.Domain.Orders;

namespace PriceForge.Infrastructure.Persistence.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public EfOrderRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _dbContext = context;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
        => await _dbContext.Orders.AddAsync(order, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
