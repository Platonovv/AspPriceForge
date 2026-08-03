using PriceForge.Domain.Common;

namespace PriceForge.Application.Carts;

public record CartQueryDto(string ProductName, int Quantity, decimal TotalPrice, Currency Currency);
