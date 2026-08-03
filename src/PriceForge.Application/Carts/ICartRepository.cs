using PriceForge.Domain.Carts;

namespace PriceForge.Application.Carts;

public interface ICartRepository
{
    Task AddItemAsync(Guid userId, Guid productId, int quantity);
    Task RemoveItemAsync(Guid userId, Guid productId);
    Task<Cart> GetAsync(Guid userId);
    Task ClearAsync(Guid userId);
}
