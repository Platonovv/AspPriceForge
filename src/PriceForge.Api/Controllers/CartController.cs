using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Contracts.Carts;
using PriceForge.Application.Carts;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.Api.Common;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/cart")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class CartController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TierService _tierService;

    public CartController(ISender sender, TierService tierService)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(tierService);

        _sender = sender;
        _tierService = tierService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartTotalQueryDto>> GetAsync(Region region, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, ct);
        CartTotalQueryDto cart = await _sender.Send(new GetCartQuery(userId, tier, region), ct);
        return Ok(cart);
    }

    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItemToCart([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        await _sender.Send(new AddToCartCommand(userId, request.ProductId, request.Quantity), ct);
        return NoContent();
    }

    [HttpDelete("items/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveItemFromCart([FromRoute] Guid productId, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        await _sender.Send(new RemoveFromCartCommand(userId, productId), ct);
        return NoContent();
    }
}
