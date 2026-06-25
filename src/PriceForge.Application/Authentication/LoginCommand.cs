using MediatR;

namespace PriceForge.Application.Authentication;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;
