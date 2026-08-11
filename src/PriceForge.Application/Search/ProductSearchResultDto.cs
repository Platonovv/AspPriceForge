namespace PriceForge.Application.Search;

public sealed class ProductSearchResultDto
{
    public List<SearchProductDto> Products { get; private set; }
    public int Total { get; private set; }

    public ProductSearchResultDto(List<SearchProductDto> products, int total)
    {
        Products = products;
        Total = total;
    }
}
