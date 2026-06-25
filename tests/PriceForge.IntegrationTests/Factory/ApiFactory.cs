using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.WebAuthTests;
using Respawn;
using Testcontainers.PostgreSql;

namespace PriceForge.IntegrationTests.Factory;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;

    public string ConnectionString => _container.GetConnectionString();

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres",
                                           "Host=localhost;Database=test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "localhost:6379,abortConnect=false");
        Environment.SetEnvironmentVariable("ConnectionStrings__RabbitMq", "amqp://guest:guest@localhost:5672/");
        Environment.SetEnvironmentVariable("ConnectionStrings__Mongo", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("ConnectionStrings__Elasticsearch", "http://localhost:9200");
        Environment.SetEnvironmentVariable("Jwt__Secret", "test_secret_key_at_least_32_chars_long_1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "PriceForge");
        Environment.SetEnvironmentVariable("Jwt__Audience", "PriceForgeUsers");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "30");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(ConnectionString));

            services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test",
                                                                             _ =>
                                                                             {
                                                                             });
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _container.StartAsync();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        _connection = new NpgsqlConnection(ConnectionString);
        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions {DbAdapter = DbAdapter.Postgres});

        await ResetAsync();
    }

    public async Task ResetAsync() => await _respawner.ResetAsync(_connection);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
