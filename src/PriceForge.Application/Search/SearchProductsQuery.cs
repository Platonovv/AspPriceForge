using MediatR;
using PriceForge.Domain.Pricing;

namespace PriceForge.Application.Search;

public sealed record SearchProductsQuery
    (string? SearchString, ProductSearchFilter ProductSearchFilter, CustomerTier Tier, Region Region)
    : IRequest<ProductSearchResultDto>;
