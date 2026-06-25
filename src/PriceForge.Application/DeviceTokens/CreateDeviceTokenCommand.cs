using MediatR;

namespace PriceForge.Application.DeviceTokens;

public sealed record CreateDeviceTokenCommand(string Token) : IRequest;
