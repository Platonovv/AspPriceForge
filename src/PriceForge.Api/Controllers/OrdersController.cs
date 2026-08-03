using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Filters;
using PriceForge.Application.Orders;
using PriceForge.Domain.Pricing;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [ServiceFilter(typeof(IdempotencyFilter))]
    [HttpPost]
    public async Task<IActionResult> Create(string? promoCode, CustomerTier tier, Region region, CancellationToken ct)
    {
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await _sender.Send(new PlaceOrderCommand(userId, promoCode, tier, region), ct);
        return Created($"/api/v1/orders/{id}", id);
    }
}
