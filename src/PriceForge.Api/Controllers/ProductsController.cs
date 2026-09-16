using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PriceForge.Api.Contracts.Products;
using PriceForge.Api.Filters;
using PriceForge.Api.Hubs;
using PriceForge.Application.Common.Exceptions;
using PriceForge.Application.Images;
using PriceForge.Application.Products;
using PriceForge.Application.Products.Images;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.Api.Common;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TierService _tierService;
    private readonly IHubContext<PriceHub> _hubContext;

    public ProductsController(ISender sender, IHubContext<PriceHub> hubContext, TierService tierService)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(hubContext);
        ArgumentNullException.ThrowIfNull(tierService);

        _sender = sender;
        _hubContext = hubContext;
        _tierService = tierService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{id}/image")]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> AssignImage(Guid id, IFormFile file, CancellationToken ct)
    {
        switch (file.Length)
        {
            case 0:
                throw new BusinessRuleException("Image is empty");
            case > 10 * 1024 * 1024:
                throw new BusinessRuleException("The file is too large");
        }

        await using Stream content = file.OpenReadStream();
        await _sender.Send(new AssignImageCommand(id, content, file.Length), ct);
        return NoContent();
    }

    [ServiceFilter(typeof(IdempotencyFilter))]
    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/products/{id}", id);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{productId}/categories")]
    public async Task<IActionResult> AssignCategory(Guid productId,
                                                    [FromBody] AssignCategoryRequest request,
                                                    CancellationToken ct)
    {
        var command = new AssignProductToCategoryCommand(productId, request.CategoryId);
        await _sender.Send(command, ct);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        ProductDto? result = await _sender.Send(new GetProductByIdQuery(id), ct);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("{id}/price")]
    public async Task<IActionResult> GetProductPriceById(Guid id, int quantity, Region region, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, ct);
        MoneyDto money = await _sender.Send(new GetProductPriceQuery(id, quantity, tier, region), ct);
        return Ok(money);
    }

    [HttpGet("price-report")]
    public async Task<IActionResult> GetProductPriceReport(CancellationToken ct)
    {
        IReadOnlyList<ProductPriceIncreaseDto> priceReportList = await _sender.Send(new GetPriceReportQuery(), ct);
        return Ok(priceReportList);
    }

    [HttpGet("{id}/image")]
    public async Task<IActionResult> GetImageUrl(Guid id, CancellationToken ct)
    {
        ImageDto imageDto = await _sender.Send(new GetImageQuery(id), ct);
        return Ok(imageDto);
    }

    [HttpPut("{id}/price")]
    public async Task<IActionResult> UpdateProductPrice(Guid id,
                                                        [FromBody] UpdateProductPriceRequest request,
                                                        CancellationToken ct)
    {
        var command = new UpdateProductPriceCommand(id, request.NewPrice, request.Currency);

        await _sender.Send(command, ct);
        await _hubContext.Clients.All.SendAsync("PriceChanged",
                                                new
                                                {
                                                    id = command.Id,
                                                    newPrice = command.NewPrice,
                                                    currency = command.Currency
                                                },
                                                ct);
        return NoContent();
    }
}
