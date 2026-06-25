using MediatR;

namespace PriceForge.Application.Products;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
