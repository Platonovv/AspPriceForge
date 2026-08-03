using MediatR;

namespace PriceForge.Application.Carts;

public sealed record RemoveFromCartCommand(Guid UserId, Guid ProductId) : IRequest;
