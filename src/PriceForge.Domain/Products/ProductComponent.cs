namespace PriceForge.Domain.Products;

public sealed class ProductComponent
{
    public Guid Id { get; private set; }
    public Guid ParentId { get; private set; }
    public Guid ChildId { get; private set; }
    public int Quantity { get; private set; }

    private ProductComponent()
    {
    }

    internal ProductComponent(Guid parentId, Guid childId, int quantity)
    {
        if (parentId == childId)
        {
            throw new ArgumentException("The child id cannot be the same as the parent id", nameof(childId));
        }

        ValidateQuantity(quantity);

        Id = Guid.NewGuid();
        ParentId = parentId;
        ChildId = childId;
        Quantity = quantity;
    }

    internal void ChangeQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
        }
    }
}
