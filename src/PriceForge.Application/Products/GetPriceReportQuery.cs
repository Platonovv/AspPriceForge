using MediatR;

namespace PriceForge.Application.Products;

public record GetPriceReportQuery : IRequest<IReadOnlyList<ProductPriceIncreaseDto>>;

