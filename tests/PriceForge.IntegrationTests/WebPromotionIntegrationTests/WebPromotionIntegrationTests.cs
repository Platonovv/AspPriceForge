using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Products;
using PriceForge.Domain.Common;
using PriceForge.Domain.Products;
using PriceForge.Domain.Promotions;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebPromotionIntegrationTests;

[Trait("Category", "Integration")]
public class WebPromotionIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebPromotionIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsAdmin_Returns201_AndPersistsPromotion()
    {
        await _factory.ResetAsync();

        // маркер для тестовой auth-схемы (→ Admin) + ключ идемпотентности (требует IdempotencyFilter)
        _client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var command = new {Sku = "E2E-1", Name = "E2E Product", Price = 1500m, Currency = "RUB"};

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

        // Assert - на то что продукт валиден
        Assert.NotNull(persisted);
        Assert.Equal(1500m, persisted.BasePrice.Amount);
        Assert.Equal(Currency.RUB, persisted.BasePrice.Currency);

        var promotionCommand = new
        {
            ProductId = persisted.Id, Percent = 25m, StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(2)
        };

        // Act — реальный HTTP через весь пайплайн (auth → controller → MediatR → repo → БД)
        HttpResponseMessage promotionResponse = await _client.PostAsJsonAsync("api/v1/promotions", promotionCommand);

        // Assert — статус
        Assert.Equal(HttpStatusCode.Created, promotionResponse.StatusCode);

        // Assert — promotion реально лёг в Postgres (проверяем напрямую в БД, не через HTTP)
        AppDbContext promotionDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Promotion? promotionloaded = await promotionDb.Promotions.FirstOrDefaultAsync(p => p.ProductId == persisted.Id);

        // Assert
        Assert.NotNull(promotionloaded);

        // Act — реальный HTTP через весь пайплайн (auth → controller → MediatR → repo → БД)
        HttpResponseMessage resp
            = await _client.GetAsync($"/api/v1/products/{id}/price?quantity=1&tier=Regular&region=Ru");
        resp.EnsureSuccessStatusCode();
        MoneyDto? money = await resp.Content.ReadFromJsonAsync<MoneyDto>(); // System.Net.Http.Json
        Assert.Equal(1125m, money!.Amount);                                 // база 1500m − промо 25%

        // Assert
        Assert.Equal(25m, promotionloaded.Percent);
        Assert.Equal(promotionCommand.StartAt, promotionloaded.StartAt, TimeSpan.FromSeconds(1));
        Assert.Equal(promotionCommand.EndAt, promotionloaded.EndAt, TimeSpan.FromSeconds(1));
        Assert.True(promotionloaded.IsActiveAt(DateTime.UtcNow.AddHours(1)));
        Assert.False(promotionloaded.IsActiveAt(DateTime.UtcNow.AddHours(3)));
        Assert.False(promotionloaded.IsActiveAt(DateTime.UtcNow.AddHours(-1)));
    }
}
