using MediatR;

namespace PriceForge.Application.Categories;

public sealed record CreateCategoryPromotionCommand(Guid CategoryId, decimal Percent, DateTime StartAt, DateTime EndAt) : IRequest<Guid>;
