using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Common;
using PriceForge.Api.Contracts.Wallets;
using PriceForge.Application.Wallets;

namespace PriceForge.Api.Controllers;

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
    public async Task<IActionResult> EarnMoney([FromBody] EarnCoinsRequest earnCoinsRequest, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        long balance = await _sender.Send(new EarnCoinsCommand(userId, earnCoinsRequest.EarnMoney), ct);

        return Ok(balance);
    }
}
