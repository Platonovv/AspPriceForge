using MediatR;

namespace PriceForge.Application.Categories;

public sealed record CreateCategoryCommand(string Name) : IRequest<Guid>;
