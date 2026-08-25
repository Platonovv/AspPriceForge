using MediatR;

namespace PriceForge.Application.Images;

public sealed record AssignImageCommand(Guid ProductId, Stream Content, long SizeBytes) : IRequest;
