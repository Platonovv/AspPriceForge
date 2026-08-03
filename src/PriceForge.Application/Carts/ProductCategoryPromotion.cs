using PriceForge.Domain.Categories;

namespace PriceForge.Application.Carts;

public sealed record ProductCategoryPromotion(Guid ProductId, CategoryPromotion Promotion);
