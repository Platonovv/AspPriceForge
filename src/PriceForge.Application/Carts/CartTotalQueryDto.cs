using PriceForge.Domain.Common;

namespace PriceForge.Application.Carts;

public record CartTotalQueryDto(List<CartQueryDto> CartQueryList, decimal TotalPrice, Currency Currency);
