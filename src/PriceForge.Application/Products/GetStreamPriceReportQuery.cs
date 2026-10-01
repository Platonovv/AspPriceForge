using MediatR;

namespace PriceForge.Application.Products;

public record GetStreamPriceReportQuery : IStreamRequest<ProductPriceIncreaseDto>;
