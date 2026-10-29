using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Domain.Common;
using PriceForge.Domain.Pricing;

namespace PriceForge.Domain.Products;

public sealed class Product
{
    public Guid Id { get; }
    public Sku Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Money BasePrice { get; private set; } = null!;
    public decimal PublicPrice { get; private set; }

    private readonly List<ProductComponent> _components = new();

    public IReadOnlyList<ProductComponent> Components => _components;

    public Product(Sku sku, string name, Money basePrice) : this(Guid.NewGuid(), sku, name, basePrice)
    {
    }

    // For EF Core materialization only
    private Product()
    {
    }

    // Все валидации и присваивания здесь
    private Product(Guid id, Sku sku, string name, Money basePrice)
    {
        if (!IsAllowedName(name))
        {
            throw new ArgumentException("Product name must be 1-200 characters", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(basePrice);

        Id = id;
        Sku = sku;
        Name = name;
        BasePrice = basePrice;
        PublicPrice = basePrice.Amount;
    }

    public override bool Equals(object? obj) => obj is Product product && Id == product.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public void Rename(string newName)
    {
        if (!IsAllowedName(newName))
        {
            throw new ArgumentException("Product New name must be 1-200 characters and not whitespace-only",
                                        nameof(newName));
        }

        Name = newName;
    }

    public void ChangeBasePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        BasePrice = newPrice;
        UpdatePublicPrice(newPrice.Amount);
    }

    public void ChangeSku(Sku newSku)
    {
        ArgumentNullException.ThrowIfNull(newSku);
        Sku = newSku;
    }

    public void AddComponent(Guid childId, int quantity)
    {
        ProductComponent? existing = FindComponent(childId);

        if (existing != null)
        {
            throw new DomainException($"Component with childId {childId} already exists");
        }

        _components.Add(new ProductComponent(Id, childId, quantity));
    }

    public void RemoveComponent(Guid childId)
    {
        ProductComponent? component = FindComponent(childId);

        if (component == null)
        {
            throw new KeyNotFoundException($"Component with childId {childId} not found in product {Id}");
        }

        _components.Remove(component);
    }

    public void ChangeComponentQuantity(Guid childId, int newQuantity)
    {
        ProductComponent? component = FindComponent(childId);

        if (component == null)
        {
            throw new KeyNotFoundException($"Component with childId {childId} not found in product {Id}");
        }

        component.ChangeQuantity(newQuantity);
    }

    public void UpdatePublicPrice(decimal amount)
    {
        amount = PriceRounding.RoundToCents(amount);

        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        if (amount > BasePrice.Amount)
        {
            throw new ArgumentException("Amount must be less than or equal to BasePrice", nameof(amount));
        }

        PublicPrice = amount;
    }

    private static bool IsAllowedName(string name)
        => !string.IsNullOrWhiteSpace(name) && name.Length is >= 1 and <= 200;

    private ProductComponent? FindComponent(Guid childId) => _components.FirstOrDefault(c => c.ChildId == childId);
}
