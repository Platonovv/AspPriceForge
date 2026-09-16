using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.PromoCodes;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.Api.Common;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/promocodes")]
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
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeCommand createPromoCodeCommand,
                                            CancellationToken ct)
    {
        Guid id = await _sender.Send(createPromoCodeCommand, ct);
        return Created($"/api/v1/promocodes/{id}", id);
    }

    [HttpGet("quote")]
    public async Task<IActionResult> QuotePromoCode(string code, Region region, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, ct);
        QuotePromoCodeDto price = await _sender.Send(new QuotePromoCodeQuery(userId, code, tier, region), ct);
        return Ok(price);
    }
}
