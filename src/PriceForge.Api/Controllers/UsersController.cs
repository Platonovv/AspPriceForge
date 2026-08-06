using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Contracts.Users;
using PriceForge.Application.Users;

namespace PriceForge.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("change-tier")]
    public async Task<IActionResult> ChangeTier([FromBody] ChangeUserTierRequest request, CancellationToken ct)
    {
        await _sender.Send(new ChangeUserTierCommand(request.UserId, request.Tier), ct);
        return NoContent();
    }
}
