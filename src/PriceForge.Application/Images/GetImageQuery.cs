using MediatR;

namespace PriceForge.Application.Images;

public record GetImageQuery(Guid ProductId) : IRequest<ImageDto>;
