using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.PromoCodes;

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

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyPromoCode([FromBody] ApplyPromoCodeCommand applyPromoCodeCommand,
                                                    CancellationToken ct)
    {
        decimal price = await _sender.Send(applyPromoCodeCommand, ct);
        return Ok(price);
    }
}
