using MediatR;

namespace PriceForge.Application.Authentication;

public sealed record LogoutCommand(Guid UserId, string Jti, DateTimeOffset ExpiresAt) : IRequest<LogoutResult>;
