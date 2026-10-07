using System.Net;
using System.Net.Http.Json;
using PriceForge.Application.Search;
using PriceForge.Domain.Pricing;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;
using PriceForge.IntegrationTests.WebProductIntegrationTests;

namespace PriceForge.IntegrationTests.WebSearchIntegrationTests;

[Trait("Category", "Integration")]
public class PublicPriceSearchTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PublicPriceSearchTests(ApiFactory factory) => _client = factory.CreateClient();

    private static string NewTerm() => $"pps{Guid.NewGuid():N}"[..16];

    private Task<Guid> CreateProductAsync(string term, string suffix, decimal price)
        => _client.CreateProductAsync($"PPS-{Guid.NewGuid():N}", $"{term} {suffix}", price);

    private async Task CreateActivePromotionAsync(Guid productId, decimal percent)
    {
        HttpResponseMessage response = await _client.SendAsync(
            ProductRequests.AsRole(HttpMethod.Post,
                                   "/api/v1/promotions",
                                   "Admin",
                                   new
                                   {
                                       ProductId = productId,
                                       Percent = percent,
                                       StartAt = DateTime.UtcNow.AddMinutes(-1),
                                       EndAt = DateTime.UtcNow.AddHours(1)
                                   }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> RegisterUserAsync(CustomerTier tier)
    {
        HttpResponseMessage register = await _client.PostAsJsonAsync("/api/v1/auth/register",
                                                                     new
                                                                     {
                                                                         Email = $"pps-{Guid.NewGuid():N}@priceforge.test",
                                                                         Password = TestUserExtensions.DefaultPassword
                                                                     });
        register.EnsureSuccessStatusCode();
        Guid userId = await register.Content.ReadFromJsonAsync<Guid>();

        if (tier != CustomerTier.Regular)
        {
            HttpResponseMessage change = await _client.SendAsync(
                ProductRequests.AsRole(HttpMethod.Post,
                                       "/api/v1/users/change-tier",
                                       "Admin",
                                       new {UserId = userId, Tier = (int)tier}));
            Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        }

        return userId;
    }

    private async Task<ProductSearchResultDto> SearchAsync(Guid userId, string query)
    {
        HttpRequestMessage request = ProductRequests.AsRole(HttpMethod.Get, $"/api/v1/search?{query}&region=Ru", "User");
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ProductSearchResultDto>())!;
    }

    [Fact]
    public async Task MaxPrice_IncludesProductWhoseDiscountedPriceFits()
    {
        string term = NewTerm();
        Guid discounted = await CreateProductAsync(term, "discounted", 5500m);
        await CreateActivePromotionAsync(discounted, 15m);
        await CreateProductAsync(term, "expensive", 5200m);
        Guid user = await RegisterUserAsync(CustomerTier.Regular);

        ProductSearchResultDto result = await SearchAsync(user, $"searchString={term}&maxPrice=5000");

        SearchProductDto product = Assert.Single(result.Products);
        Assert.EndsWith("discounted", product.Name);
        Assert.Equal(4675m, product.Price);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task SortAscending_OrdersByDiscountedPrice()
    {
        string term = NewTerm();
        Guid discounted = await CreateProductAsync(term, "discounted", 5500m);
        await CreateActivePromotionAsync(discounted, 15m);
        await CreateProductAsync(term, "plain", 5200m);
        Guid user = await RegisterUserAsync(CustomerTier.Regular);

        ProductSearchResultDto result = await SearchAsync(user, $"searchString={term}");

        Assert.Equal(new[] {4675m, 5200m}, result.Products.Select(p => p.Price).ToArray());
    }

    [Fact]
    public async Task Vip_MaxPriceIsComparedWithVipPrice()
    {
        string term = NewTerm();
        Guid discounted = await CreateProductAsync(term, "discounted", 5500m);
        await CreateActivePromotionAsync(discounted, 15m);
        Guid vip = await RegisterUserAsync(CustomerTier.Vip);

        ProductSearchResultDto result = await SearchAsync(vip, $"searchString={term}&maxPrice=4000");

        SearchProductDto product = Assert.Single(result.Products);
        Assert.Equal(3973.75m, product.Price);
    }
}
