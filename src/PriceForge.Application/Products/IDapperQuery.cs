namespace PriceForge.Application.Products;

public interface IDapperQuery
{
    Task<IReadOnlyList<ProductPriceIncreaseDto>> GetPriceIncreasesAsync(CancellationToken ct);
    Task<ProductReadDto?> GetByIdAsync(Guid id, CancellationToken ct);
    IAsyncEnumerable<ProductPriceIncreaseDto> StreamPriceIncreasesAsync(CancellationToken ct);
}
