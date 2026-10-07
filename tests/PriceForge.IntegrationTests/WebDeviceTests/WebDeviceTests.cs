using System.Net;
using System.Net.Http.Json;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebDeviceTests;

[Trait("Category", "Integration")]
public class WebDeviceTests : IClassFixture<ApiFactory>
{
    private const string DevicesUrl = "/api/v1/devices";

    private readonly HttpClient _client;

    public WebDeviceTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task RegisterDevice_Anonymous_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(DevicesUrl, new {Token = "anonymous-device-token"});

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDevice_Authenticated_ReturnsNoContent()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, DevicesUrl)
        {
            Content = JsonContent.Create(new {Token = $"device-{Guid.NewGuid():N}"})
        };
        request.Headers.Add(TestAuthHandler.AuthHeader, "true");

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
