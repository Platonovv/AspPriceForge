using MediatR;

namespace PriceForge.Application.Users;

public record ConfirmEmailCommand(string Token) : IRequest;
