using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.PromoCodes;
using PriceForge.Domain.Pricing;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/promocodes")]
public class PromoCodesController : ControllerBase
{
    private readonly ISender _sender;

    public PromoCodesController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeCommand createPromoCodeCommand,
                                            CancellationToken ct)
    {
        Guid id = await _sender.Send(createPromoCodeCommand, ct);
        return Created($"/api/v1/promocodes/{id}", id);
    }

    [HttpGet("quote")]
    public async Task<IActionResult> QuotePromoCode(string code, CustomerTier tier, Region region, CancellationToken ct)
    {
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        QuotePromoCodeDto price = await _sender.Send(new QuotePromoCodeQuery(userId, code, tier, region), ct);
        return Ok(price);
    }
}
