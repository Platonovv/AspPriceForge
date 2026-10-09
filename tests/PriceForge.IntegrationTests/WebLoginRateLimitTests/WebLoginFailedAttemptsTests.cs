using System.Net;
using System.Net.Http.Json;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebLoginRateLimitTests;

[Trait("Category", "Integration")]
public class WebLoginFailedAttemptsTests : IClassFixture<ApiFactory>
{
    private const int MaxFailedAttempts = 5;
    private const string WrongPassword = "Wrong-Passw0rd!";

    private static int _ipCounter;

    private readonly HttpClient _client;
    private readonly string _testIp;

    public WebLoginFailedAttemptsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _testIp = $"10.2.0.{Interlocked.Increment(ref _ipCounter)}";
    }

    private static string NewEmail() => $"login-{Guid.NewGuid():N}@priceforge.test";

    private async Task RegisterAsync(string email)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/auth/register",
                                                                     new {Email = email, Password = TestUserExtensions.DefaultPassword});
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpStatusCode> LoginAsync(string email, string password, string? remoteIp = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new {Email = email, Password = password})
        };
        request.Headers.Add(TestRemoteIpStartupFilter.RemoteIpHeader, remoteIp ?? _testIp);

        HttpResponseMessage response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task FailAsync(string email, int times, Func<int, string>? remoteIp = null)
    {
        for (int i = 0; i < times; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email, WrongPassword, remoteIp?.Invoke(i)));
        }
    }

    [Fact]
    public async Task AfterMaxFailures_EvenCorrectPasswordIsRejected()
    {
        string email = NewEmail();
        await RegisterAsync(email);
        await FailAsync(email, MaxFailedAttempts);

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync(email, TestUserExtensions.DefaultPassword));
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsFailedAttempts()
    {
        string email = NewEmail();
        await RegisterAsync(email);
        await FailAsync(email, MaxFailedAttempts - 1);
        Assert.Equal(HttpStatusCode.OK, await LoginAsync(email, TestUserExtensions.DefaultPassword));

        await FailAsync(email, MaxFailedAttempts - 1);

        Assert.Equal(HttpStatusCode.OK, await LoginAsync(email, TestUserExtensions.DefaultPassword));
    }

    [Fact]
    public async Task UnknownEmail_IsLimitedTheSameWay()
    {
        string email = NewEmail();
        await FailAsync(email, MaxFailedAttempts);

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync(email, WrongPassword));
    }

    [Fact]
    public async Task FailuresFromManyIps_StillLockTheAccount()
    {
        string email = NewEmail();
        await RegisterAsync(email);
        await FailAsync(email, MaxFailedAttempts, i => $"10.3.0.{i + 1}");

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync(email, TestUserExtensions.DefaultPassword, "10.3.0.200"));
    }

    [Fact]
    public async Task AnotherAccount_IsNotAffected()
    {
        string victim = NewEmail();
        string other = NewEmail();
        await RegisterAsync(victim);
        await RegisterAsync(other);
        await FailAsync(victim, MaxFailedAttempts);

        Assert.Equal(HttpStatusCode.OK, await LoginAsync(other, TestUserExtensions.DefaultPassword));
    }

    [Fact]
    public async Task EmailCase_DoesNotBypassTheLimit()
    {
        string email = NewEmail();
        await RegisterAsync(email);
        await FailAsync(email, MaxFailedAttempts - 1);

        Assert.Equal(HttpStatusCode.Unauthorized, await LoginAsync(email.ToUpperInvariant(), WrongPassword));
        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync(email, TestUserExtensions.DefaultPassword));
    }
}
