using Microsoft.EntityFrameworkCore;
using PriceForge.Game.Application.Items;
using PriceForge.Game.Domain.Items;

namespace PriceForge.Game.Infrastructure.Persistence.Repositories;

internal sealed class EfGameItemRepository : IGameItemRepository
{
    private readonly GameDbContext _dbContext;

    public EfGameItemRepository(GameDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<GameItem?> GetByProductIdAsync(Guid productId, CancellationToken ct)
        => _dbContext.GameItems.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId, ct);
}
