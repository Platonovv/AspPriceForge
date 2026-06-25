using MediatR;

namespace PriceForge.Application.Products;

public sealed class GetPriceReportHandler : IRequestHandler<GetPriceReportQuery, IReadOnlyList<ProductPriceIncreaseDto>>
{
    private readonly IDapperQuery _dapperQuery;

    public GetPriceReportHandler(IDapperQuery dapperQuery)
    {
        ArgumentNullException.ThrowIfNull(dapperQuery);
        _dapperQuery = dapperQuery;
    }

    public async Task<IReadOnlyList<ProductPriceIncreaseDto>> Handle(GetPriceReportQuery request,
                                                                     CancellationToken cancellationToken)
    {
        return await _dapperQuery.GetPriceIncreasesAsync(cancellationToken);
    }
}
