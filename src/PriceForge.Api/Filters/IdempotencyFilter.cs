using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Primitives;

namespace PriceForge.Api.Filters;

public sealed class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IDistributedCache _cache;

    public IdempotencyFilter(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        StringValues requestHeader = context.HttpContext.Request.Headers["Idempotency-Key"];

        if (StringValues.IsNullOrEmpty(requestHeader))
        {
            context.Result = new BadRequestResult();
            return;
        }

        string key = "idempotency:" + requestHeader;
        string? cached = await _cache.GetStringAsync(key);

        if (cached != null)
        {
            context.Result = new ObjectResult(cached);
        }
        else
        {
            ActionExecutedContext executed = await next();
            if (executed.Result is CreatedResult created)
            {
                await _cache.SetStringAsync(key,
                                            created.Value?.ToString() ?? "",
                                            options: new DistributedCacheEntryOptions
                                            {
                                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                                            });
            }
        }
    }
}
