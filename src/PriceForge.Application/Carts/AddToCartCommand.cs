using MediatR;

namespace PriceForge.Application.Carts;

public sealed record AddToCartCommand(Guid UserId, Guid ProductId, int Quantity) : IRequest;
