using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Common;
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
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("change-tier")]
    public async Task<IActionResult> ChangeTier([FromBody] ChangeUserTierRequest request, CancellationToken ct)
    {
        await _sender.Send(new ChangeUserTierCommand(request.UserId, request.Tier), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("send-confirm-email")]
    public async Task<IActionResult> SendConfirmEmail(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        await _sender.Send(new SendConfirmEmailCommand(userId), ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        await _sender.Send(new ConfirmEmailCommand(request.Token), ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _sender.Send(new ForgotPasswordCommand(request.Email), ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _sender.Send(new ResetPasswordCommand(request.Token, request.NewPassword), ct);

        return NoContent();
    }
}
