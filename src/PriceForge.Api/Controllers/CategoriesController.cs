using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Contracts.Categories;
using PriceForge.Application.Categories;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/categories")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        _sender = sender;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/categories/{id}", id);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{categoryId}/promotions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategoryPromotion(Guid categoryId,
                                                             [FromBody] CreateCategoryPromotionRequest request,
                                                             CancellationToken ct)
    {
        var command = new CreateCategoryPromotionCommand(categoryId, request.Percent, request.StartAt, request.EndAt);
        Guid id = await _sender.Send(command, ct);

        return Created($"/api/v1/categories/{categoryId}/promotions/{id}", id);
    }
}
