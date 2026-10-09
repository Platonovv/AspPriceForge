using System.Net;
using System.Net.Http.Json;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebLoginRateLimitTests;

[Trait("Category", "Integration")]
public class WebLoginRateLimitTests : IClassFixture<ApiFactory>
{
    private const int PermitLimit = 20;

    private readonly HttpClient _client;

    public WebLoginRateLimitTests(ApiFactory factory) => _client = factory.CreateClient();

    private async Task<HttpStatusCode> LoginAsync(string remoteIp, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new {Email = $"nobody-{Guid.NewGuid():N}@priceforge.test", Password = "Wrong-Passw0rd!"})
        };
        request.Headers.Add(TestRemoteIpStartupFilter.RemoteIpHeader, remoteIp);

        if (forwardedFor != null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        HttpResponseMessage response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task ExhaustAsync(string remoteIp, string? forwardedFor = null)
    {
        for (int i = 0; i < PermitLimit; i++)
        {
            Assert.NotEqual(HttpStatusCode.TooManyRequests, await LoginAsync(remoteIp, forwardedFor));
        }
    }

    [Fact]
    public async Task SameIp_OverLimit_IsTooManyRequests()
    {
        await ExhaustAsync("10.1.1.1");

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync("10.1.1.1"));
    }

    [Fact]
    public async Task AnotherIp_IsNotAffectedByExhaustedOne()
    {
        await ExhaustAsync("10.1.1.2");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, await LoginAsync("10.1.1.3"));
    }

    [Fact]
    public async Task SpoofedForwardedFor_FromUntrustedConnection_IsIgnored()
    {
        for (int i = 0; i < PermitLimit; i++)
        {
            Assert.NotEqual(HttpStatusCode.TooManyRequests, await LoginAsync("10.1.1.4", $"203.0.113.{i}"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync("10.1.1.4", "203.0.113.250"));
    }

    [Fact]
    public async Task MappedAndPlainIpv4_ShareOneBucket()
    {
        for (int i = 0; i < PermitLimit / 2; i++)
        {
            await LoginAsync("::ffff:10.1.1.5");
            await LoginAsync("10.1.1.5");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync("10.1.1.5"));
    }

    [Fact]
    public async Task ThroughTrustedProxy_ClientsAreLimitedSeparately()
    {
        await ExhaustAsync(ApiFactory.TrustedProxyIp, "198.51.100.1");

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginAsync(ApiFactory.TrustedProxyIp, "198.51.100.1"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await LoginAsync(ApiFactory.TrustedProxyIp, "198.51.100.2"));
    }
}
