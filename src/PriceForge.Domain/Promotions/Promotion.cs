namespace PriceForge.Domain.Promotions;

public sealed class Promotion
{
    public Guid Id { get; }
    public Guid ProductId { get; }
    public decimal Percent { get; }
    public DateTime StartAt { get; }
    public DateTime EndAt { get; }

    public Promotion(Guid productId, decimal percent, DateTime startAt, DateTime endAt) : this(
        Guid.NewGuid(),
        productId,
        percent,
        startAt,
        endAt)
    {
    }

    private Promotion()
    {
    }

    private Promotion(Guid id, Guid productId, decimal percent, DateTime startAt, DateTime endAt)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Promotion product id cannot be empty", nameof(productId));
        }

        if (percent is > 90 or <= 0)
        {
            throw new ArgumentException("Promotion percent must be in range (0, 90]", nameof(percent));
        }

        if (startAt == default)
        {
            throw new ArgumentException("Promotion start at must be set", nameof(startAt));
        }

        if (endAt == default)
        {
            throw new ArgumentException("Promotion end at must be set", nameof(endAt));
        }

        if (endAt <= startAt)
        {
            throw new ArgumentException("Promotion endAt must be greater than startAt", nameof(endAt));
        }

        Id = id;
        ProductId = productId;
        Percent = percent;
        StartAt = startAt;
        EndAt = endAt;
    }

    public bool IsActiveAt(DateTime now) => now >= StartAt && now <= EndAt;
}
