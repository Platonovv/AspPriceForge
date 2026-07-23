namespace PriceForge.Domain.Categories;

public sealed class ProductCategory
{
    public Guid ProductId { get; private set; }
    public Guid CategoryId { get; private set; }

    private ProductCategory()
    {
    }

    public ProductCategory(Guid productId, Guid categoryId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("product id not be null", nameof(productId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("category id not be null", nameof(categoryId));
        }

        ProductId = productId;
        CategoryId = categoryId;
    }
}
