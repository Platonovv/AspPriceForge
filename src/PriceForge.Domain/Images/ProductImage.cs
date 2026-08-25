namespace PriceForge.Domain.Images;

public sealed class ProductImage
{
    public Guid Id { get; }

    public Guid ProductId { get; }

    public string Key { get; private set; }

    public string ContentType { get; private set; }

    public long SizeBytes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ProductImage(Guid productId, string key, string contentType, long sizeBytes) : this(
        Guid.NewGuid(),
        productId,
        key,
        contentType,
        sizeBytes)
    {
    }

    // For EF Core materialization only
    private ProductImage()
    {
    }

    private ProductImage(Guid id, Guid productId, string key, string contentType, long sizeBytes)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException($"Product Id cannot be an empty guid.", nameof(productId));
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException($"Product Key cannot be an empty string.", nameof(key));
        }

        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentException($"Content Type cannot be an empty string.", nameof(contentType));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentException($"Size Bytes cannot be zero or negative.", nameof(sizeBytes));
        }

        Id = id;
        ProductId = productId;
        Key = key;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Replace(string key, string contentType, long sizeBytes)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException($"Key cannot be an empty string.", nameof(key));
        }

        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentException($"Content Type cannot be an empty string.", nameof(contentType));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentException($"Size Bytes cannot be zero or negative.", nameof(sizeBytes));
        }

        Key = key;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        CreatedAt = DateTime.UtcNow;
    }
}
