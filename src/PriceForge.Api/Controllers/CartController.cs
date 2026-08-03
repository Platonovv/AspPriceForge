using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Contracts.Carts;
using PriceForge.Application.Carts;
using PriceForge.Domain.Pricing;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/cart")]
public class CartController : ControllerBase
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult> GetAsync(CustomerTier tier, Region region, CancellationToken ct)
    {
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        CartTotalQueryDto cart = await _sender.Send(new GetCartQuery(userId, tier, region), ct);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItemToCart([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _sender.Send(new AddToCartCommand(userId, request.ProductId, request.Quantity), ct);
        return NoContent();
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItemFromCart([FromRoute] Guid productId, CancellationToken ct)
    {
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _sender.Send(new RemoveFromCartCommand(userId, productId), ct);
        return NoContent();
    }
}
