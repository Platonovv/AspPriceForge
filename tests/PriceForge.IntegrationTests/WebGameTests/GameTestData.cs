using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Game.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebGameTests;

public static class GameTestData
{
    public static readonly Guid GoldenSwordId = Guid.Parse("0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01");
    public static readonly Guid SpeedBoostId = Guid.Parse("0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02");
    public const long SpeedBoostPrice = 150;

    public static async Task<HttpResponseMessage> EarnAsync(this HttpClient client, long coins, string? idempotencyKey = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/wallet/earn");
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(new {Coins = coins});

        return await client.SendAsync(request);
    }

    public static async Task SeedGameItemsAsync(this ApiFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        GameDbContext db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO products (id, sku, name, base_price_amount, base_price_currency, public_price)
            VALUES ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', 'GAME-GOLDEN-SWORD', 'Golden Sword', 1.99, 'USD', 1.99),
                   ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02', 'GAME-SPEED-BOOST', 'Speed Boost', 0.99, 'USD', 0.99)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO game.game_items (product_id, coin_price)
            VALUES ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c01', 500),
                   ('0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02', 150)
            ON CONFLICT (product_id) DO NOTHING;
            """);
    }
}
