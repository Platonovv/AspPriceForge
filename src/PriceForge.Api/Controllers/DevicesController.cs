using MediatR;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Application.DeviceTokens;

namespace PriceForge.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
public class DevicesController : ControllerBase
{
    private readonly ISender _sender;

    public DevicesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceTokenCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        return NoContent();
    }
}
