namespace PriceForge.Domain.Categories;

public sealed class CategoryPromotion
{
    public Guid Id { get; }
    public Guid CategoryId { get; }
    public decimal Percent { get; }
    public DateTime StartAt { get; }
    public DateTime EndAt { get; }

    public CategoryPromotion(Guid categoryId, decimal percent, DateTime startAt, DateTime endAt) : this(
        Guid.NewGuid(),
        categoryId,
        percent,
        startAt,
        endAt)
    {
    }

    private CategoryPromotion()
    {
    }

    private CategoryPromotion(Guid id, Guid categoryId, decimal percent, DateTime startAt, DateTime endAt)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category product id cannot be empty", nameof(categoryId));
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
        CategoryId = categoryId;
        Percent = percent;
        StartAt = startAt;
        EndAt = endAt;
    }

    public bool IsActiveAt(DateTime now) => now >= StartAt && now <= EndAt;
}
