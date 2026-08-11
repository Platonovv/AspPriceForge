using System.Net;
using System.Net.Http.Json;
using PriceForge.Application.Search;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebSearchIntegrationTests;

[Trait("Category", "Integration")]
public class WebSearchIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebSearchIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, "true");
    }

    private async Task RegisterAndImpersonateAsync(string email)
    {
        HttpResponseMessage register
            = await _client.PostAsJsonAsync("/api/v1/auth/register", new {Email = email, Password = "Passw0rd!"});
        register.EnsureSuccessStatusCode();

        Guid userId = await register.Content.ReadFromJsonAsync<Guid>();

        _client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        _client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
    }

    private async Task<Guid> CreateProductAsync(string sku, string name, decimal price)
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products",
                                                                     new
                                                                     {
                                                                         Sku = sku,
                                                                         Name = name,
                                                                         Price = price,
                                                                         Currency = "RUB"
                                                                     });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task SeedProductsAsync(int count, string namePrefix, decimal startPrice)
    {
        for (int i = 1; i <= count; i++)
        {
            await CreateProductAsync($"SEARCH-{i}", $"{namePrefix} {i}", startPrice + i);
        }
    }

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/categories", new {Name = name});
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task AssignCategoryAsync(Guid productId, Guid categoryId)
    {
        HttpResponseMessage response
            = await _client.PostAsJsonAsync($"/api/v1/products/{productId}/categories", new {CategoryId = categoryId});
        response.EnsureSuccessStatusCode();
    }

    private async Task CreatePromotionAsync(Guid productId, decimal percent)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/promotions",
                                                                     new
                                                                     {
                                                                         ProductId = productId,
                                                                         Percent = percent,
                                                                         StartAt = DateTime.UtcNow.AddHours(-1),
                                                                         EndAt = DateTime.UtcNow.AddHours(1)
                                                                     });
        response.EnsureSuccessStatusCode();
    }

    private async Task<ProductSearchResultDto> SearchAsync(string query)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/search?{query}");
        response.EnsureSuccessStatusCode();

        ProductSearchResultDto? result = await response.Content.ReadFromJsonAsync<ProductSearchResultDto>();

        Assert.NotNull(result);

        return result;
    }

    [Fact]
    public async Task Search_LastPage_ReturnsRemainderAndFullTotal()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-paging@priceforge.test");
        await SeedProductsAsync(25, "Наушники", 1000m);

        string term = Uri.EscapeDataString("наушники");

        ProductSearchResultDto firstPage = await SearchAsync($"searchString={term}&pageIndex=1&pageSize=10&region=Ru");
        ProductSearchResultDto lastPage = await SearchAsync($"searchString={term}&pageIndex=3&pageSize=10&region=Ru");

        Assert.Equal(10, firstPage.Products.Count);
        Assert.Equal(5, lastPage.Products.Count);
        Assert.Equal(25, firstPage.Total);
        Assert.Equal(25, lastPage.Total);

        string[] firstSkus = firstPage.Products.Select(p => p.Sku).ToArray();
        string[] lastSkus = lastPage.Products.Select(p => p.Sku).ToArray();

        Assert.Empty(firstSkus.Intersect(lastSkus));
    }

    [Fact]
    public async Task Search_UpperCaseTerm_FindsLowerCaseNames()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-case@priceforge.test");
        await SeedProductsAsync(25, "Наушники", 1000m);

        string term = Uri.EscapeDataString("НАУШН");

        ProductSearchResultDto page = await SearchAsync($"searchString={term}&pageIndex=1&pageSize=10&region=Ru");

        Assert.Equal(10, page.Products.Count);
        Assert.Equal(25, page.Total);
    }

    [Fact]
    public async Task Search_BySku_ReturnsSingleProduct()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-sku@priceforge.test");
        await SeedProductsAsync(25, "Наушники", 1000m);

        string term = Uri.EscapeDataString("SEARCH-1");

        ProductSearchResultDto page = await SearchAsync($"searchString={term}&pageIndex=1&pageSize=10&region=Ru");

        Assert.Single(page.Products);
        Assert.Equal("SEARCH-1", page.Products[0].Sku);
    }

    [Fact]
    public async Task Search_TermThatCannotBeSku_ReturnsEmptyWithoutError()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-badsku@priceforge.test");
        await SeedProductsAsync(25, "Наушники", 1000m);

        string term = Uri.EscapeDataString("Науш 1212");

        ProductSearchResultDto page = await SearchAsync($"searchString={term}&pageIndex=1&pageSize=10&region=Ru");

        Assert.Empty(page.Products);
        Assert.Equal(0, page.Total);
    }

    [Fact]
    public async Task Search_MaxPrice_ExcludesMoreExpensiveProducts()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-price@priceforge.test");
        await SeedProductsAsync(25, "Наушники", 1000m);

        string term = Uri.EscapeDataString("Наушн");

        ProductSearchResultDto page
            = await SearchAsync($"searchString={term}&maxPrice=1010&pageIndex=1&pageSize=20&region=Ru");

        Assert.Equal(10, page.Total);
        Assert.Equal(10, page.Products.Count);
        Assert.All(page.Products, p => Assert.True(p.Price <= 1010m));
    }

    [Fact]
    public async Task Search_Descending_ReversesOrder()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-sort@priceforge.test");
        await SeedProductsAsync(25, "Наушники", 1000m);

        string term = Uri.EscapeDataString("Наушн");

        ProductSearchResultDto ascending
            = await SearchAsync($"searchString={term}&descending=false&pageIndex=1&pageSize=10&region=Ru");
        ProductSearchResultDto descending
            = await SearchAsync($"searchString={term}&descending=true&pageIndex=1&pageSize=10&region=Ru");

        Assert.Equal(1001m, ascending.Products[0].Price);
        Assert.Equal(1025m, descending.Products[0].Price);
    }

    [Fact]
    public async Task Search_ByCategory_ReturnsOnlyProductsOfThatCategory()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-category@priceforge.test");

        Guid inCategory = await CreateProductAsync("CAT-1", "Наушники беспроводные", 1000m);
        await CreateProductAsync("CAT-2", "Наушники проводные", 2000m);

        Guid categoryId = await CreateCategoryAsync("Электроника");
        await AssignCategoryAsync(inCategory, categoryId);

        string term = Uri.EscapeDataString("Наушники");

        ProductSearchResultDto page
            = await SearchAsync($"searchString={term}&category={categoryId}&pageIndex=1&pageSize=10&region=Ru");

        Assert.Single(page.Products);
        Assert.Equal("CAT-1", page.Products[0].Sku);
    }

    [Fact]
    public async Task Search_ReturnsPriceWithActivePromotionApplied()
    {
        await _factory.ResetAsync();

        await RegisterAndImpersonateAsync("search-promo@priceforge.test");

        Guid productId = await CreateProductAsync("PROMO-1", "Наушники студийные", 1000m);
        await CreatePromotionAsync(productId, 25m);

        string term = Uri.EscapeDataString("студийные");

        ProductSearchResultDto page = await SearchAsync($"searchString={term}&pageIndex=1&pageSize=10&region=Ru");

        Assert.Single(page.Products);
        Assert.Equal(750m, page.Products[0].Price);
    }

    [Fact]
    public async Task Search_WithoutAuthentication_Returns401()
    {
        await _factory.ResetAsync();

        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/v1/search?pageIndex=1&pageSize=10&region=Ru");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
