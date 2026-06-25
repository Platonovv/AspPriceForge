using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace PriceForge.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        StringValues incoming = context.Request.Headers["X-Correlation-Id"];
        string id = StringValues.IsNullOrEmpty(incoming) ? Guid.NewGuid().ToString() : incoming.ToString();
        context.Response.Headers["X-Correlation-Id"] = id;
        using (LogContext.PushProperty("CorrelationId", id))
        {
            await _next(context);
        }
    }
}
