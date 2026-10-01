using System.Data.Common;

namespace PriceForge.Application.Common;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct);
}
