using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PriceForge.Application.Authentication;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebTokenRevocationTests;

[Trait("Category", "Integration")]
public class WebTokenRevocationTests : IClassFixture<JwtApiFactory>
{
    private readonly HttpClient _client;
    private readonly JwtApiFactory _factory;

    public WebTokenRevocationTests(JwtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_RevokesAccessImmediately_AndKillsRefresh()
    {
        await _factory.ResetAsync();

        const string email = "revocation@priceforge.test";
        const string password = "Passw0rd!";

        HttpResponseMessage register
            = await _client.PostAsJsonAsync("/api/v1/auth/register", new {Email = email, Password = password});
        register.EnsureSuccessStatusCode();

        HttpResponseMessage login
            = await _client.PostAsJsonAsync("/api/v1/auth/login", new {Email = email, Password = password});
        login.EnsureSuccessStatusCode();

        LoginResult tokens = (await login.Content.ReadFromJsonAsync<LoginResult>())!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        HttpResponseMessage meBefore = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meBefore.StatusCode);

        HttpResponseMessage logout = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        HttpResponseMessage meAfter = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfter.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new {tokens.RefreshToken});
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task RevokedToken_IsRejectedOnEveryAuthorizedEndpoint()
    {
        await _factory.ResetAsync();

        const string email = "revocation-twice@priceforge.test";
        const string password = "Passw0rd!";

        HttpResponseMessage register
            = await _client.PostAsJsonAsync("/api/v1/auth/register", new {Email = email, Password = password});
        register.EnsureSuccessStatusCode();

        HttpResponseMessage login
            = await _client.PostAsJsonAsync("/api/v1/auth/login", new {Email = email, Password = password});
        login.EnsureSuccessStatusCode();

        LoginResult tokens = (await login.Content.ReadFromJsonAsync<LoginResult>())!;

        HttpClient second = _factory.CreateClient();
        second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        HttpResponseMessage first = await second.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        HttpResponseMessage repeat = await second.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, repeat.StatusCode);
    }
}
