using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PriceForge.BuildingBlocks.Security;
using PriceForge.BuildingBlocks.Web.Filters;
using PriceForge.Game.Api.Requests;
using PriceForge.Game.Application.Purchases;

namespace PriceForge.Game.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/game")]
public class GameController : ControllerBase
{
    private readonly ISender _sender;

    public GameController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [ServiceFilter(typeof(IdempotencyFilter))]
    [HttpPost("purchases")]
    [ProducesResponseType(typeof(CoinPurchaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PurchaseItem([FromBody] BuyGameItemRequest buyGameItemRequest,
                                                  [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
                                                  CancellationToken cancellationToken)
    {
        CoinPurchaseDto dto
            = await _sender.Send(new BuyGameItemCommand(User.GetUserId(), buyGameItemRequest.ProductId, idempotencyKey),
                                 cancellationToken);
        return Created($"/api/v1/game/purchases/{dto.PurchaseId}", dto);
    }
}
