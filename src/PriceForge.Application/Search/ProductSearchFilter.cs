namespace PriceForge.Application.Search;

public sealed record ProductSearchFilter
    (Guid? Category, decimal? MaxPrice, int PageIndex = 1, int PageSize = 20, bool Descending = false);
