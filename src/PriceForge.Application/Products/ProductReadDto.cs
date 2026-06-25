using PriceForge.Domain.Common;
using PriceForge.Domain.Products;

namespace PriceForge.Application.Products;

public sealed record ProductReadDto(Guid Id, string Name, Sku Sku, decimal BasePriceAmount, Currency BasePriceCurrency);
