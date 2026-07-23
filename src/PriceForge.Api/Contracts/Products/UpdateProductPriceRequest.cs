namespace PriceForge.Api.Contracts.Products;

public sealed record UpdateProductPriceRequest(decimal NewPrice, string Currency);
