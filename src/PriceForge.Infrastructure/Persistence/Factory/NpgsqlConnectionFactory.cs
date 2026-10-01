using Microsoft.Extensions.Configuration;
using System.Data.Common;
using Npgsql;
using PriceForge.Application.Common;

namespace PriceForge.Infrastructure.Persistence.Factory;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;


    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")!;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

}
