using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.Promotions;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/promotions")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class PromotionController : ControllerBase
{
    private readonly ISender _sender;

    public PromotionController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/promotions/{id}", id);
    }
}
