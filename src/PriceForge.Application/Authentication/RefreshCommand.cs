using MediatR;

namespace PriceForge.Application.Authentication;

public sealed record RefreshCommand(string RefreshToken) : IRequest<LoginResult>;
