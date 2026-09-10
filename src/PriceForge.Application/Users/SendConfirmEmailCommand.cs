using MediatR;

namespace PriceForge.Application.Users;

public record SendConfirmEmailCommand(Guid UserId) : IRequest;
