using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class WebProductPersistenceTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebProductPersistenceTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAdmin_Returns201_AndPersistsProduct()
    {
        await _factory.ResetAsync();

        // маркер для тестовой auth-схемы (→ Admin) + ключ идемпотентности (требует IdempotencyFilter)
        _client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var command = new
        {
            Sku = "E2E-1",
            Name = "E2E Product",
            Price = 1500m,
            Currency = "RUB"
        };

        // Act — реальный HTTP через весь пайплайн (auth → controller → MediatR → repo → БД)
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products", command);

        // Assert — статус
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // id из тела ответа
        Guid id = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert — продукт реально лёг в Postgres (проверяем напрямую в БД, не через HTTP)
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Product? persisted = await db.Products.FirstOrDefaultAsync(p => p.Id == id);

        Assert.NotNull(persisted);
        Assert.Equal(1500m, persisted!.BasePrice.Amount);
        Assert.Equal(Currency.RUB, persisted.BasePrice.Currency);
    }
}
