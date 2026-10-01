using System.Net.Http.Json;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

internal static class ProductRequests
{
    public static HttpRequestMessage AsRole(HttpMethod method, string url, string role, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.AuthHeader, "true");
        request.Headers.Add(TestAuthHandler.RoleHeader, role);

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    public static async Task<Guid> CreateProductAsync(this HttpClient client, string sku, string name, decimal price)
    {
        HttpRequestMessage request = AsRole(HttpMethod.Post,
                                            "/api/v1/products",
                                            "Admin",
                                            new {Sku = sku, Name = name, Price = price, Currency = "RUB"});
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
