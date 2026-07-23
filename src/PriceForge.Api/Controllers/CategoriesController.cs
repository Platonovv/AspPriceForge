using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceForge.Api.Contracts.Categories;
using PriceForge.Application.Categories;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/categories/{id}", id);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{categoryId}/promotions")]
    public async Task<IActionResult> CreateCategoryPromotion(Guid categoryId,
                                                             [FromBody] CreateCategoryPromotionRequest request,
                                                             CancellationToken ct)
    {
        var command = new CreateCategoryPromotionCommand(categoryId, request.Percent, request.StartAt, request.EndAt);
        Guid id = await _sender.Send(command, ct);

        return Created($"/api/v1/categories/{categoryId}/promotions/{id}", id);
    }
}
