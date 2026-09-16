using PriceForge.Domain.Categories;

namespace PriceForge.Application.Common;

public sealed record ProductCategoryPromotion(Guid ProductId, CategoryPromotion Promotion);
