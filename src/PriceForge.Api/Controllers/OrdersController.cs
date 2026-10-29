using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Filters;
using PriceForge.Application.Orders;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.BuildingBlocks.Security;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TierService _tierService;

    public OrdersController(ISender sender, TierService tierService)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(tierService);

        _sender = sender;
        _tierService = tierService;
    }

    [ServiceFilter(typeof(IdempotencyFilter))]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(string? promoCode, Region region, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, ct);
        Guid id = await _sender.Send(new PlaceOrderCommand(userId, promoCode, tier, region), ct);
        return Created($"/api/v1/orders/{id}", id);
    }
}
