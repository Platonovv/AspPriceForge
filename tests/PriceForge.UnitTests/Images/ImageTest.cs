using PriceForge.Application.Products.Images;

namespace PriceForge.UnitTests.Images;

[Trait("Category", "Unit")]
public class ImageTest
{
    [Fact]
    public async Task DetectAsync_JpegHeader_ReturnsJpegFormat()
    {
        var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0x11, 0x22]);

        ImageFormat? imageFormat = await ImageSignature.DetectAsync(stream, CancellationToken.None);

        Assert.NotNull(imageFormat);
        Assert.Equal("jpg", imageFormat.Extension);
        Assert.Equal("image/jpeg", imageFormat.ContentType);
    }
}
