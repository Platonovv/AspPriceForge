using MediatR;

namespace PriceForge.Application.Products;

public sealed record AssignProductToCategoryCommand(Guid ProductId, Guid CategoryId) : IRequest;
