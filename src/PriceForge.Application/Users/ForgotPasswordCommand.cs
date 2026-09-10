using MediatR;

namespace PriceForge.Application.Users;

public record ForgotPasswordCommand(string Email) : IRequest;
