namespace PriceForge.Api.Contracts.Categories;

public sealed record CreateCategoryPromotionRequest(decimal Percent, DateTime StartAt, DateTime EndAt);
