using System.Net;
using System.Text;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebProductIntegrationTests;

[Trait("Category", "Integration")]
public class PriceReportExportTests : IClassFixture<ApiFactory>
{
    private const string ExportUrl = "/api/v1/products/price-report/export";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public PriceReportExportTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ChangePriceAsync(Guid productId, decimal newPrice)
    {
        HttpRequestMessage request = ProductRequests.AsRole(HttpMethod.Put,
                                                            $"/api/v1/products/{productId}/price",
                                                            "Admin",
                                                            new {NewPrice = newPrice, Currency = "RUB"});

        HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Export_AsAdmin_ReturnsCsvFileWithEscapedNames()
    {
        await _factory.ResetAsync();

        Guid pen = await _client.CreateProductAsync("CSV-PEN", "Ручка, синяя", 100m);
        Guid laptop = await _client.CreateProductAsync("CSV-LAPTOP", "Ноутбук 15\" Pro", 1000m);

        await ChangePriceAsync(pen, 150m);
        await ChangePriceAsync(laptop, 1030m);

        HttpResponseMessage response = await _client.SendAsync(ProductRequests.AsRole(HttpMethod.Get, ExportUrl, "Admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("price-report.csv", response.Content.Headers.ContentDisposition?.FileName);

        byte[] body = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(Encoding.UTF8.Preamble.ToArray(), body[..3]);

        string[] lines = Encoding.UTF8.GetString(body[3..])
                                 .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(line => line.TrimEnd('\r'))
                                 .ToArray();

        Assert.Equal(new[]
                     {
                         "Name,Summary",
                         "\"Ручка, синяя\",50.00",
                         "\"Ноутбук 15\"\" Pro\",30.00"
                     },
                     lines);
    }

    [Fact]
    public async Task Export_AsRegularUser_IsForbidden()
    {
        HttpResponseMessage response = await _client.SendAsync(ProductRequests.AsRole(HttpMethod.Get, ExportUrl, "User"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_Anonymous_IsUnauthorizedWithProblemDetails()
    {
        HttpResponseMessage response = await _client.GetAsync(ExportUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
