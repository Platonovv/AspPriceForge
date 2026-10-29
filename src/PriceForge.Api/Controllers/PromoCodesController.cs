using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.PromoCodes;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.BuildingBlocks.Security;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/promocodes")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class PromoCodesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TierService _tierService;

    public PromoCodesController(ISender sender, TierService tierService)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(tierService);

        _sender = sender;
        _tierService = tierService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeCommand createPromoCodeCommand,
                                            CancellationToken ct)
    {
        Guid id = await _sender.Send(createPromoCodeCommand, ct);
        return Created($"/api/v1/promocodes/{id}", id);
    }

    [HttpGet("quote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuotePromoCodeDto>> QuotePromoCode(string code, Region region, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, ct);
        QuotePromoCodeDto price = await _sender.Send(new QuotePromoCodeQuery(userId, code, tier, region), ct);
        return Ok(price);
    }
}
