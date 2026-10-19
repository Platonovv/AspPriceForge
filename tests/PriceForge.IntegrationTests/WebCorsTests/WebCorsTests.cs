using System.Net;
using System.Net.Http.Json;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebCorsTests;

[Trait("Category", "Integration")]
public class WebCorsTests : IClassFixture<ApiFactory>
{
    private const string AllowOrigin = "Access-Control-Allow-Origin";
    private const string UnknownOrigin = "http://evil.example";
    private const int LoginPermitLimit = 20;

    private readonly HttpClient _client;

    public WebCorsTests(ApiFactory factory) => _client = factory.CreateClient();

    private static HttpRequestMessage CreatePurchasePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/orders");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization, content-type, idempotency-key");
        return request;
    }

    private static string? GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : null;

    [Fact]
    public async Task PurchasePreflight_FromGameOrigin_IsAllowed()
    {
        HttpResponseMessage response = await _client.SendAsync(CreatePurchasePreflight(ApiFactory.AllowedGameOrigin));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ApiFactory.AllowedGameOrigin, GetHeader(response, AllowOrigin));
        Assert.Contains("POST", GetHeader(response, "Access-Control-Allow-Methods") ?? "", StringComparison.OrdinalIgnoreCase);

        string allowedHeaders = GetHeader(response, "Access-Control-Allow-Headers") ?? "";
        Assert.Contains("authorization", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("content-type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("idempotency-key", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchasePreflight_FromUnknownOrigin_IsNotAllowed()
    {
        HttpResponseMessage response = await _client.SendAsync(CreatePurchasePreflight(UnknownOrigin));

        Assert.Null(GetHeader(response, AllowOrigin));
    }

    [Fact]
    public async Task UnauthorizedResponse_ForGameOrigin_IsReadableByBrowser()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/products/{Guid.NewGuid()}");
        request.Headers.Add("Origin", ApiFactory.AllowedGameOrigin);

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ApiFactory.AllowedGameOrigin, GetHeader(response, AllowOrigin));
    }

    [Fact]
    public async Task RateLimitedResponse_ForGameOrigin_IsReadableByBrowser()
    {
        HttpResponseMessage response = null!;

        for (int i = 0; i <= LoginPermitLimit; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new {Email = $"nobody-{Guid.NewGuid():N}@priceforge.test", Password = "Wrong-Passw0rd!"})
            };
            request.Headers.Add("Origin", ApiFactory.AllowedGameOrigin);
            request.Headers.Add(TestRemoteIpStartupFilter.RemoteIpHeader, "203.0.113.77");
            response = await _client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(ApiFactory.AllowedGameOrigin, GetHeader(response, AllowOrigin));
    }
}
