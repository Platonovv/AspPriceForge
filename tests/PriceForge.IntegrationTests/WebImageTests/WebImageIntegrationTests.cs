using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceForge.Application.Images;
using PriceForge.Domain.Images;
using PriceForge.Infrastructure.Persistence;
using PriceForge.IntegrationTests.Factory;
using PriceForge.IntegrationTests.WebAuthTests;

namespace PriceForge.IntegrationTests.WebImageTests;

[Trait("Category", "Integration")]
public class WebImageIntegrationTests : IClassFixture<ApiFactory>
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0x11, 0x22, 0x33];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    private static readonly byte[] TextBytes = "not an image at all"u8.ToArray();

    private static readonly HttpClient DownloadClient = new();

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public WebImageIntegrationTests(ApiFactory factory)
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

    private async Task<Guid> CreateProductAsync(string sku)
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products",
                                                                     new
                                                                     {
                                                                         Sku = sku,
                                                                         Name = $"Product {sku}",
                                                                         Price = 1500m,
                                                                         Currency = "RUB"
                                                                     });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<HttpResponseMessage> UploadImageAsync(Guid productId, byte[] bytes, string declaredType)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(declaredType);

        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "photo.jpg");

        return await _client.PostAsync($"/api/v1/products/{productId}/image", form);
    }

    private async Task<ProductImage?> LoadImageRecordAsync(Guid productId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.ProductImages.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    [Fact]
    public async Task AssignImage_ValidJpeg_StoresRecordAndFile()
    {
        await _factory.ResetAsync();
        await RegisterAndImpersonateAsync("image-upload@priceforge.test");

        Guid productId = await CreateProductAsync("IMG-1");

        HttpResponseMessage upload = await UploadImageAsync(productId, JpegBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.NoContent, upload.StatusCode);

        ProductImage? record = await LoadImageRecordAsync(productId);

        Assert.NotNull(record);
        Assert.StartsWith($"products/{productId}/", record.Key);
        Assert.EndsWith(".jpg", record.Key);
        Assert.Equal("image/jpeg", record.ContentType);
        Assert.Equal(JpegBytes.Length, record.SizeBytes);

        Assert.True(await _factory.ObjectExistsAsync(record.Key));
    }

    [Fact]
    public async Task AssignImage_PngContent_IsDetectedBySignature()
    {
        await _factory.ResetAsync();
        await RegisterAndImpersonateAsync("image-png@priceforge.test");

        Guid productId = await CreateProductAsync("IMG-2");

        HttpResponseMessage upload = await UploadImageAsync(productId, PngBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.NoContent, upload.StatusCode);

        ProductImage? record = await LoadImageRecordAsync(productId);

        Assert.NotNull(record);
        Assert.Equal("image/png", record.ContentType);
        Assert.EndsWith(".png", record.Key);
    }

    [Fact]
    public async Task AssignImage_NotAnImage_ReturnsBadRequest()
    {
        await _factory.ResetAsync();
        await RegisterAndImpersonateAsync("image-invalid@priceforge.test");

        Guid productId = await CreateProductAsync("IMG-3");

        HttpResponseMessage upload = await UploadImageAsync(productId, TextBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Null(await LoadImageRecordAsync(productId));
    }

    [Fact]
    public async Task AssignImage_Twice_ReplacesRecordAndRemovesOldFile()
    {
        await _factory.ResetAsync();
        await RegisterAndImpersonateAsync("image-replace@priceforge.test");

        Guid productId = await CreateProductAsync("IMG-4");

        await UploadImageAsync(productId, JpegBytes, "image/jpeg");

        ProductImage? first = await LoadImageRecordAsync(productId);

        Assert.NotNull(first);

        await UploadImageAsync(productId, PngBytes, "image/png");

        ProductImage? second = await LoadImageRecordAsync(productId);

        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);
        Assert.NotEqual(first.Key, second.Key);

        Assert.True(await _factory.ObjectExistsAsync(second.Key));
        Assert.False(await _factory.ObjectExistsAsync(first.Key));
    }

    [Fact]
    public async Task GetImage_AfterUpload_ReturnsUrlThatServesTheFile()
    {
        await _factory.ResetAsync();
        await RegisterAndImpersonateAsync("image-download@priceforge.test");

        Guid productId = await CreateProductAsync("IMG-5");

        await UploadImageAsync(productId, JpegBytes, "image/jpeg");

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/products/{productId}/image");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImageDto? dto = await response.Content.ReadFromJsonAsync<ImageDto>();

        Assert.NotNull(dto);
        Assert.Equal("image/jpeg", dto.ContentType);

        byte[] downloaded = await DownloadClient.GetByteArrayAsync(dto.Url);

        Assert.Equal(JpegBytes, downloaded);
    }

    [Fact]
    public async Task GetImage_WithoutUploadedImage_ReturnsNotFound()
    {
        await _factory.ResetAsync();
        await RegisterAndImpersonateAsync("image-missing@priceforge.test");

        Guid productId = await CreateProductAsync("IMG-6");

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/products/{productId}/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
