using MediatR;

namespace PriceForge.Application.Authentication;

public sealed record RegisterCommand(string Email, string Password) : IRequest<Guid>;
