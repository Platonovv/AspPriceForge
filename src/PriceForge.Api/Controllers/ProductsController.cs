using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PriceForge.Api.Contracts.Products;
using PriceForge.Api.Filters;
using PriceForge.Api.Hubs;
using PriceForge.BuildingBlocks.Exceptions;
using PriceForge.Application.Images;
using PriceForge.Application.Products;
using PriceForge.Application.Users;
using PriceForge.Domain.Pricing;
using PriceForge.Api.Common;
using PriceForge.BuildingBlocks.Security;

namespace PriceForge.Api.Controllers;

[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/products/{id}", id);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{productId}/categories")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignCategory(Guid productId,
                                                    [FromBody] AssignCategoryRequest request,
                                                    CancellationToken ct)
    {
        var command = new AssignProductToCategoryCommand(productId, request.CategoryId);
        await _sender.Send(command, ct);
        return NoContent();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct)
    {
        ProductDto? result = await _sender.Send(new GetProductByIdQuery(id), ct);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("{id}/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MoneyDto>> GetProductPriceById(Guid id,
                                                                  int quantity,
                                                                  Region region,
                                                                  CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        CustomerTier tier = await _tierService.GetCustomerTierAsync(userId, ct);
        MoneyDto money = await _sender.Send(new GetProductPriceQuery(id, quantity, tier, region), ct);
        return Ok(money);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("price-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ProductPriceIncreaseDto>>> GetProductPriceReport(CancellationToken ct)
    {
        IReadOnlyList<ProductPriceIncreaseDto> priceReportList = await _sender.Send(new GetPriceReportQuery(), ct);
        return Ok(priceReportList);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("price-report/export")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task GetProductPriceReportExport(CancellationToken ct)
    {
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=price-report.csv";

        await using var writer = new StreamWriter(Response.Body,
                                                  new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                                                  leaveOpen: true);

        IAsyncEnumerable<ProductPriceIncreaseDto> priceReportList
            = _sender.CreateStream(new GetStreamPriceReportQuery(), ct);

        await writer.WriteLineAsync("Name,Summary");

        await foreach (ProductPriceIncreaseDto row in priceReportList)
        {
            await writer.WriteLineAsync($"{row.Name.EscapeCsv()},{row.Summary.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    [HttpGet("{id}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImageDto>> GetImageUrl(Guid id, CancellationToken ct)
    {
        ImageDto imageDto = await _sender.Send(new GetImageQuery(id), ct);
        return Ok(imageDto);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id}/price")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
