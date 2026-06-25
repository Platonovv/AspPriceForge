using MediatR;

namespace PriceForge.Application.Products;

public sealed record CreateProductCommand(string Sku, string Name, decimal Price, string Currency) : IRequest<Guid>;
