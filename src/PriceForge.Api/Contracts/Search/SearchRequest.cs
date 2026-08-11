using PriceForge.Domain.Pricing;

namespace PriceForge.Api.Contracts.Search;

public sealed class SearchRequest
{
    public string? SearchString { get; set; }
    public Guid? Category { get; set; }
    public decimal? MaxPrice { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool Descending { get; set; }
    public Region Region { get; set; } = Region.RU;
}
