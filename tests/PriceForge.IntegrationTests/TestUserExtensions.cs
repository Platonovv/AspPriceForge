using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Domain.Users;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests;

public static class TestUserExtensions
{
    public const string DefaultPassword = "Passw0rd!";

    public static async Task<Guid> RegisterAndImpersonateAsync(this HttpClient client,
                                                               string email,
                                                               string password = DefaultPassword)
    {
        HttpResponseMessage register
            = await client.PostAsJsonAsync("/api/v1/auth/register", new {Email = email, Password = password});
        register.EnsureSuccessStatusCode();

        Guid userId = await register.Content.ReadFromJsonAsync<Guid>();

        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        return userId;
    }

    public static async Task VerifyEmailAsync(this ApiFactory factory, Guid userId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        User user = await db.Users.SingleAsync(x => x.Id == userId);
        user.EmailConfirmation();

        await db.SaveChangesAsync();
    }
}
