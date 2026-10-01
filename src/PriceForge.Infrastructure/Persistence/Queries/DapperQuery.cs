using System.Data.Common;
using System.Runtime.CompilerServices;
using Dapper;
using PriceForge.Application.Common;
using PriceForge.Application.Products;

namespace PriceForge.Infrastructure.Persistence.Queries;

public class DapperQuery : IDapperQuery
{
    private const string SqlPrice = @"SELECT p.name AS Name, SUM(pa.new_price - pa.old_price) AS Summary
                     FROM products p
                     JOIN product_price_audit pa ON p.id = pa.product_id
                     GROUP BY p.id, p.name
                     ORDER BY Summary DESC";
    private readonly IDbConnectionFactory _factory;

    public DapperQuery(IDbConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public async IAsyncEnumerable<ProductPriceIncreaseDto> StreamPriceIncreasesAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using DbConnection conn = await _factory.CreateOpenConnectionAsync(ct);

        IAsyncEnumerable<ProductPriceIncreaseDto> result = conn.QueryUnbufferedAsync<ProductPriceIncreaseDto>(SqlPrice);

        await foreach (ProductPriceIncreaseDto row in result.WithCancellation(ct))
        {
            yield return row;
        }
    }

    public async Task<IReadOnlyList<ProductPriceIncreaseDto>> GetPriceIncreasesAsync(CancellationToken ct)
    {
        await using DbConnection conn = await _factory.CreateOpenConnectionAsync(ct);

        IEnumerable<ProductPriceIncreaseDto> result
            = await conn.QueryAsync<ProductPriceIncreaseDto>(new CommandDefinition(SqlPrice, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<ProductReadDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using DbConnection connection = await _factory.CreateOpenConnectionAsync(ct);
        const string sql
            = @"SELECT p.id AS Id,  p.name AS Name, p.sku AS Sku, p.base_price_amount AS BasePriceAmount, p.base_price_currency AS BasePriceCurrency
                FROM products AS p WHERE p.id = @id";

        ProductReadDto? result
            = await connection.QuerySingleOrDefaultAsync<ProductReadDto>(
                  new CommandDefinition(sql, new {id}, cancellationToken: ct));

        return result;
    }
}
