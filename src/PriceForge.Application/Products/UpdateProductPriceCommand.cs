using MediatR;

namespace PriceForge.Application.Products;

public sealed record UpdateProductPriceCommand(Guid Id, decimal NewPrice, string Currency) : IRequest;
