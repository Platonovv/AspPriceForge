namespace PriceForge.Application.Products.Images;

public static class ImageSignature
{
    private const int HeaderLength = 8;

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static async Task<ImageFormat?> DetectAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] header = new byte[HeaderLength];

        int read = await stream.ReadAtLeastAsync(header, HeaderLength, false, cancellationToken);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return Detect(header, read);
    }

    private static ImageFormat? Detect(byte[] header, int length)
    {
        ReadOnlySpan<byte> prefix = header.AsSpan(0, length);

        if (prefix.StartsWith(Jpeg))
        {
            return new ImageFormat("jpg", "image/jpeg");
        }

        if (prefix.StartsWith(Png))
        {
            return new ImageFormat("png", "image/png");
        }

        return null;
    }
}
