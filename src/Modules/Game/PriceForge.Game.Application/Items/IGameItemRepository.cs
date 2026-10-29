using PriceForge.Game.Domain.Items;

namespace PriceForge.Game.Application.Items;

public interface IGameItemRepository
{
    Task<GameItem?> GetByProductIdAsync(Guid productId, CancellationToken ct);
}
