using MediatR;

namespace PriceForge.Application.Users;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
