using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PriceForge.Api.Filters;
using PriceForge.Api.Hubs;
using PriceForge.Application.Products;
using PriceForge.Domain.Pricing;

namespace PriceForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHubContext<PriceHub> _hubContext;

    public ProductsController(ISender sender, IHubContext<PriceHub> hubContext)
    {
        _sender = sender;
        _hubContext = hubContext;
    }

    [ServiceFilter(typeof(IdempotencyFilter))]
    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        Guid id = await _sender.Send(command, ct);
        return Created($"/api/v1/products/{id}", id);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        ProductDto? result = await _sender.Send(new GetProductByIdQuery(id), ct);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("{id}/price")]
    public async Task<IActionResult> GetProductPriceById(Guid id,
                                                         int quantity,
                                                         CustomerTier tier,
                                                         Region region,
                                                         CancellationToken ct)
    {
        MoneyDto money = await _sender.Send(new GetProductPriceQuery(id, quantity, tier, region), ct);
        return Ok(money);
    }

    [HttpGet("price-report")]
    public async Task<IActionResult> GetProductPriceReport(CancellationToken ct)
    {
        IReadOnlyList<ProductPriceIncreaseDto> priceReportList = await _sender.Send(new GetPriceReportQuery(), ct);
        return Ok(priceReportList);
    }

    [HttpPut("{id}/price")]
    public async Task<IActionResult> UpdateProductPrice(Guid id,
                                                        [FromBody] UpdateProductPriceCommand command,
                                                        CancellationToken ct)
    {
        command = command with {Id = id};

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
