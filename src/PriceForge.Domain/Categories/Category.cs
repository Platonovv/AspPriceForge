namespace PriceForge.Domain.Categories;

public sealed class Category
{
    public Guid Id { get; }
    public string Name { get; private set; } = null!;

    public Category(string name) : this(Guid.NewGuid(), name)
    {
    }

    // For EF Core materialization only
    private Category()
    {
    }

    private Category(Guid id, string name)
    {
        if (!IsAllowedName(name))
        {
            throw new ArgumentException("Category name must be 1-200 characters", nameof(name));
        }

        Id = id;
        Name = name;
    }

    public override string ToString() => Name;

    private static bool IsAllowedName(string name)
        => !string.IsNullOrWhiteSpace(name) && name.Length is >= 1 and <= 200;
}
