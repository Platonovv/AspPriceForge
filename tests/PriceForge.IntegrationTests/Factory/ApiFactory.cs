using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using PriceForge.Infrastructure.Images;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.WebAuthTests;
using Respawn;
using StackExchange.Redis;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace PriceForge.IntegrationTests.Factory;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7").Build();
    private const string MinioUser = "minioadmin";
    private const string MinioPassword = "minioadmin";
    private const string BucketName = "product-images";

    private readonly MinioContainer _minioContainer = new MinioBuilder("minio/minio")
                                                      .WithUsername(MinioUser)
                                                      .WithPassword(MinioPassword)
                                                      .Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;
    private IConnectionMultiplexer _redis = null!;
    private IAmazonS3 _s3Client = null!;
    private string _storageEndpoint = null!;

    protected virtual bool UseTestAuth => true;

    public string ConnectionString => _container.GetConnectionString();

    public string RedisConnectionString => _redisContainer.GetConnectionString();
    public string MinioConnectionString => _minioContainer.GetConnectionString();

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

            if (UseTestAuth)
            {
                services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test",
                            _ =>
                            {
                            });
            }

            services.RemoveAll<IHostedService>();
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ => _redis);

            services.AddSingleton<IOptions<StorageSettings>>(Options.Create(new StorageSettings
            {
                Endpoint = _storageEndpoint,
                PublicEndpoint = _storageEndpoint,
                AccessKey = MinioUser,
                SecretKey = MinioPassword,
                Bucket = BucketName
            }));
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _container.StartAsync();
        await _redisContainer.StartAsync();
        await _minioContainer.StartAsync();

        _redis = await ConnectionMultiplexer.ConnectAsync($"{RedisConnectionString},allowAdmin=true");

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        _connection = new NpgsqlConnection(ConnectionString);
        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions {DbAdapter = DbAdapter.Postgres});

        _storageEndpoint = $"http://{_minioContainer.Hostname}:{_minioContainer.GetMappedPublicPort(9000)}";

        Environment.SetEnvironmentVariable("Storage__Endpoint", _storageEndpoint);
        Environment.SetEnvironmentVariable("Storage__PublicEndpoint", _storageEndpoint);
        Environment.SetEnvironmentVariable("Storage__AccessKey", MinioUser);
        Environment.SetEnvironmentVariable("Storage__SecretKey", MinioPassword);
        Environment.SetEnvironmentVariable("Storage__Bucket", BucketName);

        _s3Client = new AmazonS3Client(new BasicAWSCredentials(MinioUser, MinioPassword),
                                       new AmazonS3Config {ServiceURL = _storageEndpoint, ForcePathStyle = true});

        await _s3Client.PutBucketAsync(new PutBucketRequest {BucketName = BucketName});

        await ResetAsync();
    }

    public async Task PutObjectAsync(string key, byte[] content)
    {
        using var stream = new MemoryStream(content);

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            ContentType = "image/jpeg"
        });
    }

    public async Task<bool> ObjectExistsAsync(string key)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(BucketName, key);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_connection);

        EndPoint endPoint = _redis.GetEndPoints().First();
        await _redis.GetServer(endPoint).FlushDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _s3Client.Dispose();
        await _connection.DisposeAsync();
        await _redis.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await _container.DisposeAsync();
        await _minioContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
