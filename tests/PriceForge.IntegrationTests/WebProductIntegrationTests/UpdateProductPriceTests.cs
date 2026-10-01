using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.Products;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class UpdateProductPriceTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public UpdateProductPriceTests(ApiFactory factory) => _client = factory.CreateClient();

    private static string PriceUrl(Guid productId) => $"/api/v1/products/{productId}/price";

    private Task<Guid> CreateProductAsync(decimal price) =>
        _client.CreateProductAsync($"UPD-{Guid.NewGuid():N}", "Товар для смены цены", price);

    private async Task<decimal> GetPriceAsync(Guid productId)
    {
        HttpResponseMessage response = await _client.SendAsync(
            ProductRequests.AsRole(HttpMethod.Get, $"/api/v1/products/{productId}", "Admin"));
        response.EnsureSuccessStatusCode();

        ProductDto? product = await response.Content.ReadFromJsonAsync<ProductDto>();
        return product!.Price;
    }

    [Fact]
    public async Task UpdatePrice_AsRegularUser_IsForbiddenAndPriceUnchanged()
    {
        Guid productId = await CreateProductAsync(100m);

        HttpResponseMessage response = await _client.SendAsync(
            ProductRequests.AsRole(HttpMethod.Put, PriceUrl(productId), "User", new {NewPrice = 1m, Currency = "RUB"}));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(100m, await GetPriceAsync(productId));
    }

    [Fact]
    public async Task UpdatePrice_AsAdmin_ChangesPrice()
    {
        Guid productId = await CreateProductAsync(100m);

        HttpResponseMessage response = await _client.SendAsync(
            ProductRequests.AsRole(HttpMethod.Put, PriceUrl(productId), "Admin", new {NewPrice = 150m, Currency = "RUB"}));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(150m, await GetPriceAsync(productId));
    }

    [Theory]
    [InlineData(150, "XYZ")]
    [InlineData(150, "999")]
    [InlineData(-1, "RUB")]
    public async Task UpdatePrice_InvalidRequest_ReturnsBadRequestAndPriceUnchanged(decimal newPrice, string currency)
    {
        Guid productId = await CreateProductAsync(100m);

        HttpResponseMessage response = await _client.SendAsync(
            ProductRequests.AsRole(HttpMethod.Put, PriceUrl(productId), "Admin", new {NewPrice = newPrice, Currency = currency}));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.False(string.IsNullOrWhiteSpace(problem?.Detail));

        Assert.Equal(100m, await GetPriceAsync(productId));
    }
}
