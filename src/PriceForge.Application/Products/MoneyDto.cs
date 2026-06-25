using PriceForge.Domain.Common;

namespace PriceForge.Application.Products;

public sealed record MoneyDto(decimal Amount, Currency Currency);
