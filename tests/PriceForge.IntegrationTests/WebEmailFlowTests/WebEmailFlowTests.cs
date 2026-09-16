using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Authentication;
using PriceForge.Application.TokenGenerators;
using PriceForge.Domain.Users;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebEmailFlowTests;

[Trait("Category", "Integration")]
public class WebEmailFlowTests : IClassFixture<JwtApiFactory>
{
    private const string Password = "Passw0rd!";

    private readonly JwtApiFactory _factory;
    private readonly HttpClient _client;

    public WebEmailFlowTests(JwtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> RegisterAsync(string email)
    {
        HttpResponseMessage register
            = await _client.PostAsJsonAsync("/api/v1/auth/register", new {Email = email, Password});
        register.EnsureSuccessStatusCode();

        return await register.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<LoginResult> LoginAsync(string email, string password)
    {
        HttpResponseMessage login
            = await _client.PostAsJsonAsync("/api/v1/auth/login", new {Email = email, Password = password});
        login.EnsureSuccessStatusCode();

        return (await login.Content.ReadFromJsonAsync<LoginResult>())!;
    }

    private async Task<string> SeedTokenAsync(Guid userId, UserTokenType tokenType, TimeSpan? lifetime = null)
    {
        GeneratedToken generated = TokenGenerator.GenerateToken();

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.UserTokens.Add(new UserToken(userId,
                                        generated.Hash,
                                        tokenType,
                                        DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(30))));
        await db.SaveChangesAsync();

        return generated.Token;
    }

    private async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await query(db);
    }

    [Fact]
    public async Task SendConfirmEmail_StoresTokenAndOutboxMessage()
    {
        await _factory.ResetAsync();

        const string email = "confirm-request@priceforge.test";
        Guid userId = await RegisterAsync(email);
        LoginResult tokens = await LoginAsync(email, Password);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        HttpResponseMessage response = await _client.PostAsync("/api/v1/users/send-confirm-email", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        UserToken token = await QueryAsync(db => db.UserTokens.SingleAsync(x => x.UserId == userId));
        Assert.Equal(UserTokenType.ConfirmedEmail, token.TokenType);
        Assert.Null(token.UsedAt);

        bool outboxHasMessage = await QueryAsync(db => db.OutboxMessages
                                                         .AnyAsync(x => x.Type == "email.confirmation.requested"));
        Assert.True(outboxHasMessage);
    }

    [Fact]
    public async Task ConfirmEmail_MarksUserVerified_AndBurnsToken()
    {
        await _factory.ResetAsync();

        Guid userId = await RegisterAsync("confirm-ok@priceforge.test");
        string token = await SeedTokenAsync(userId, UserTokenType.ConfirmedEmail);

        HttpResponseMessage response
            = await _client.PostAsJsonAsync("/api/v1/users/confirm-email", new {Token = token});

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        bool verified = await QueryAsync(db => db.Users.Where(x => x.Id == userId)
                                                 .Select(x => x.IsEmailVerified)
                                                 .SingleAsync());
        Assert.True(verified);

        DateTime? usedAt = await QueryAsync(db => db.UserTokens.Where(x => x.UserId == userId)
                                                    .Select(x => x.UsedAt)
                                                    .SingleAsync());
        Assert.NotNull(usedAt);
    }

    [Fact]
    public async Task ConfirmEmail_SameTokenTwice_IsRejected()
    {
        await _factory.ResetAsync();

        Guid userId = await RegisterAsync("confirm-twice@priceforge.test");
        string token = await SeedTokenAsync(userId, UserTokenType.ConfirmedEmail);

        HttpResponseMessage first = await _client.PostAsJsonAsync("/api/v1/users/confirm-email", new {Token = token});
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        HttpResponseMessage second = await _client.PostAsJsonAsync("/api/v1/users/confirm-email", new {Token = token});
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_TokenOfAnotherType_IsRejected()
    {
        await _factory.ResetAsync();

        Guid userId = await RegisterAsync("confirm-wrong-type@priceforge.test");
        string resetToken = await SeedTokenAsync(userId, UserTokenType.ChangePassword);

        HttpResponseMessage response
            = await _client.PostAsJsonAsync("/api/v1/users/confirm-email", new {Token = resetToken});

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        bool verified = await QueryAsync(db => db.Users.Where(x => x.Id == userId)
                                                 .Select(x => x.IsEmailVerified)
                                                 .SingleAsync());
        Assert.False(verified);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_LooksIdenticalAndLeavesNoTrace()
    {
        await _factory.ResetAsync();

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/users/forgot-password",
                                                                     new {Email = "nobody@priceforge.test"});

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        int tokens = await QueryAsync(db => db.UserTokens.CountAsync());
        int messages = await QueryAsync(db => db.OutboxMessages.CountAsync());

        Assert.Equal(0, tokens);
        Assert.Equal(0, messages);
    }

    [Fact]
    public async Task ForgotPassword_OverLimitWithinWindow_ReturnsTooManyRequests()
    {
        await _factory.ResetAsync();

        const string email = "ratelimit@priceforge.test";
        await RegisterAsync(email);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            HttpResponseMessage allowed
                = await _client.PostAsJsonAsync("/api/v1/users/forgot-password", new {Email = email});
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        }

        HttpResponseMessage blocked
            = await _client.PostAsJsonAsync("/api/v1/users/forgot-password", new {Email = email});

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ChangesPassword_AndRevokesEverythingIssuedBefore()
    {
        await _factory.ResetAsync();

        const string email = "reset@priceforge.test";
        const string newPassword = "NewPassw0rd!";

        Guid userId = await RegisterAsync(email);
        LoginResult oldTokens = await LoginAsync(email, Password);

        string resetToken = await SeedTokenAsync(userId, UserTokenType.ChangePassword);
        string staleResetToken = await SeedTokenAsync(userId, UserTokenType.ChangePassword);

        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/v1/users/reset-password",
                                                                  new {Token = resetToken, NewPassword = newPassword});
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        HttpResponseMessage oldPassword
            = await _client.PostAsJsonAsync("/api/v1/auth/login", new {Email = email, Password});
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        HttpResponseMessage newPasswordLogin
            = await _client.PostAsJsonAsync("/api/v1/auth/login", new {Email = email, Password = newPassword});
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);

        HttpResponseMessage refresh
            = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new {oldTokens.RefreshToken});
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldTokens.AccessToken);

        HttpResponseMessage me = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage staleToken
            = await _client.PostAsJsonAsync("/api/v1/users/reset-password",
                                            new {Token = staleResetToken, NewPassword = "AnotherPassw0rd!"});
        Assert.Equal(HttpStatusCode.BadRequest, staleToken.StatusCode);
    }
}
