using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Contracts.Search;
using PriceForge.Application.Search;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.Api.Common;

namespace PriceForge.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/search")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class SearchController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TierService _tierService;

    public SearchController(ISender sender, TierService tierService)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(tierService);

        _sender = sender;
        _tierService = tierService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductSearchResultDto>> Search([FromQuery] SearchRequest searchRequest,
                                            CancellationToken cancellationToken)
    {
        var productSearchFilter = new ProductSearchFilter(searchRequest.Category,
                                                          searchRequest.MaxPrice,
                                                          searchRequest.PageIndex,
                                                          searchRequest.PageSize,
                                                          searchRequest.Descending);
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, cancellationToken);
        var query = new SearchProductsQuery(searchRequest.SearchString,
                                            productSearchFilter,
                                            tier,
                                            searchRequest.Region);

        ProductSearchResultDto response = await _sender.Send(query, cancellationToken);
        return Ok(response);
    }
}
