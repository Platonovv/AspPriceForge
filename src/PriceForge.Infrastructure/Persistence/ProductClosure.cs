namespace PriceForge.Infrastructure.Persistence;

public sealed class ProductClosure
{
    public Guid AncestorId { get; set; }
    public Guid DescendantId { get; set; }
    public int Depth { get; set; }
}
