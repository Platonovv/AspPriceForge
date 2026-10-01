using MediatR;

namespace PriceForge.Application.Products;

public sealed class
    GetStreamPriceReportHandler : IStreamRequestHandler<GetStreamPriceReportQuery, ProductPriceIncreaseDto>
{
    private readonly IDapperQuery _dapperQuery;

    public GetStreamPriceReportHandler(IDapperQuery dapperQuery)
    {
        ArgumentNullException.ThrowIfNull(dapperQuery);
        _dapperQuery = dapperQuery;
    }

    public IAsyncEnumerable<ProductPriceIncreaseDto> Handle(GetStreamPriceReportQuery request,
                                                            CancellationToken cancellationToken)
        => _dapperQuery.StreamPriceIncreasesAsync(cancellationToken);
}
