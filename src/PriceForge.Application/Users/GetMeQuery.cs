using MediatR;

namespace PriceForge.Application.Users;

public sealed record GetMeQuery(Guid UserId) : IRequest<MeDto>;
