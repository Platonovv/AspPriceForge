namespace PriceForge.Application.Products;

public record ProductDto(Guid Id, string Sku, string Name, decimal Price, string Currency);
