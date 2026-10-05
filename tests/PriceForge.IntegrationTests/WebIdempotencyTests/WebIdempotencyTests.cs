using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;
using PriceForge.IntegrationTests.WebProductIntegrationTests;

namespace PriceForge.IntegrationTests.WebIdempotencyTests;

[Trait("Category", "Integration")]
public class WebIdempotencyTests : IClassFixture<ApiFactory>
{
    private const string ProductsUrl = "/api/v1/products";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebIdempotencyTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage CreateProductRequest(string idempotencyKey, Guid userId, string name)
    {
        HttpRequestMessage request = ProductRequests.AsRole(HttpMethod.Post,
                                                            ProductsUrl,
                                                            "Admin",
                                                            new
                                                            {
                                                                Sku = $"IDEM-{Guid.NewGuid():N}",
                                                                Name = name,
                                                                Price = 100m,
                                                                Currency = "RUB"
                                                            });
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        return request;
    }

    private async Task<int> CountProductsAsync(string name)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Products.CountAsync(p => p.Name == name);
    }

    [Fact]
    public async Task Create_SameKeyTwice_ReturnsSameCreatedResponseAndCreatesOnce()
    {
        string key = Guid.NewGuid().ToString();
        Guid userId = Guid.NewGuid();
        string name = $"Idempotent {Guid.NewGuid():N}";

        HttpResponseMessage first = await _client.SendAsync(CreateProductRequest(key, userId, name));
        HttpResponseMessage replay = await _client.SendAsync(CreateProductRequest(key, userId, name));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal("application/json", replay.Content.Headers.ContentType?.MediaType);
        Assert.Equal(first.Headers.Location, replay.Headers.Location);
        Assert.Equal(await first.Content.ReadFromJsonAsync<Guid>(), await replay.Content.ReadFromJsonAsync<Guid>());
        Assert.Equal(1, await CountProductsAsync(name));
    }

    [Fact]
    public async Task Create_SameKeyFromDifferentUsers_CreatesSeparateProducts()
    {
        string key = Guid.NewGuid().ToString();
        string firstName = $"First {Guid.NewGuid():N}";
        string secondName = $"Second {Guid.NewGuid():N}";

        HttpResponseMessage first = await _client.SendAsync(CreateProductRequest(key, Guid.NewGuid(), firstName));
        HttpResponseMessage second = await _client.SendAsync(CreateProductRequest(key, Guid.NewGuid(), secondName));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotEqual(await first.Content.ReadFromJsonAsync<Guid>(), await second.Content.ReadFromJsonAsync<Guid>());
        Assert.Equal(1, await CountProductsAsync(firstName));
        Assert.Equal(1, await CountProductsAsync(secondName));
    }

    [Fact]
    public async Task SameKeyOnAnotherEndpoint_IsNotAnsweredFromFirstEndpointCache()
    {
        HttpResponseMessage register = await _client.PostAsJsonAsync("/api/v1/auth/register",
                                                                     new
                                                                     {
                                                                         Email = $"idem-{Guid.NewGuid():N}@priceforge.test",
                                                                         Password = TestUserExtensions.DefaultPassword
                                                                     });
        register.EnsureSuccessStatusCode();
        Guid userId = await register.Content.ReadFromJsonAsync<Guid>();
        await _factory.VerifyEmailAsync(userId);

        string key = Guid.NewGuid().ToString();

        HttpResponseMessage product = await _client.SendAsync(CreateProductRequest(key, userId, $"Endpoint {Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);

        HttpRequestMessage order = ProductRequests.AsRole(HttpMethod.Post, "/api/v1/orders?region=Ru", "Admin");
        order.Headers.Add("Idempotency-Key", key);
        order.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        HttpResponseMessage orderResponse = await _client.SendAsync(order);

        Assert.Equal(HttpStatusCode.BadRequest, orderResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ConcurrentRequestsWithSameKey_ExecuteOnce()
    {
        string key = Guid.NewGuid().ToString();
        Guid userId = Guid.NewGuid();
        string name = $"Concurrent {Guid.NewGuid():N}";

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => _client.SendAsync(CreateProductRequest(key, userId, name))));

        Assert.Equal(1, await CountProductsAsync(name));
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] {HttpStatusCode.Created, HttpStatusCode.Conflict}));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithoutKey_ReturnsBadRequestProblem()
    {
        HttpRequestMessage request = ProductRequests.AsRole(HttpMethod.Post,
                                                            ProductsUrl,
                                                            "Admin",
                                                            new {Sku = "IDEM-NOKEY", Name = "No key", Price = 1m, Currency = "RUB"});

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
