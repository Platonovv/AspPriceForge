using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PriceForge.Application.Authentication;
using PriceForge.Application.Users;

namespace PriceForge.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/auth/{id}", id);
    }

    [EnableRateLimiting("EnterLogin")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        LoginResult result = await _sender.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand command, CancellationToken ct)
    {
        LoginResult result = await _sender.Send(command, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        string jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var expiredAt
            = DateTimeOffset.FromUnixTimeSeconds(long.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Exp)!));

        var command = new LogoutCommand(userId, jti, expiredAt);

        LogoutResult result = await _sender.Send(command, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        MeDto me = await _sender.Send(new GetMeQuery(userId), ct);
        return Ok(me);
    }
}
