using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PriceForge.BuildingBlocks.Security;
using PriceForge.Game.Api.Requests;
using PriceForge.Game.Application.Wallets;

namespace PriceForge.Game.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/wallet")]
public class WalletController : ControllerBase
{
    private readonly ISender _sender;

    public WalletController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        long balance = await _sender.Send(new GetWalletBalanceQuery(userId), ct);

        return Ok(balance);
    }

    [HttpPost("earn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EarnCoins([FromBody] EarnCoinsRequest earnCoinsRequest,
                                               [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
                                               CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        long balance = await _sender.Send(new EarnCoinsCommand(userId, idempotencyKey, earnCoinsRequest.Coins), ct);

        return Ok(balance);
    }
}
