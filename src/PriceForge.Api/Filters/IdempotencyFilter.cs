using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PriceForge.Api.Common;
using PriceForge.Application.Common.Exceptions;
using StackExchange.Redis;

namespace PriceForge.Api.Filters;

public sealed class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IDatabase _cacheDatabase;
    private readonly IOptions<JsonOptions> _options;

    public IdempotencyFilter(IConnectionMultiplexer multiplexer, IOptions<JsonOptions> options)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentNullException.ThrowIfNull(options);

        _cacheDatabase = multiplexer.GetDatabase();
        _options = options;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        StringValues idempotencyKey = context.HttpContext.Request.Headers["Idempotency-Key"];

        if (StringValues.IsNullOrEmpty(idempotencyKey))
        {
            context.Result = new BadRequestResult();
            return;
        }

        Guid userId = context.HttpContext.User.GetUserId();
        string requestMethod = context.HttpContext.Request.Method;
        PathString requestPath = context.HttpContext.Request.Path;

        RedisKey key = $"idempotency:{userId}:{requestMethod}:{requestPath}:{idempotencyKey}";
        RedisValue redisCacheValue = await _cacheDatabase.StringGetAsync(key);
        RedisKey lockKey = $"{key}:lock";

        if (GetRedisCacheValue(context, redisCacheValue))
        {
            return;
        }

        if (!await _cacheDatabase.StringSetAsync(lockKey, "1", TimeSpan.FromSeconds(30), When.NotExists))
        {
            throw new ConflictException("A request with this key is already in progress");
        }

        try
        {
            redisCacheValue = await _cacheDatabase.StringGetAsync(key);

            if (GetRedisCacheValue(context, redisCacheValue))
            {
                return;
            }

            ActionExecutedContext executed = await next();

            if (executed.Result is CreatedResult created)
            {
                var cachedResponse = new CachedCreatedResponse(created.Location,
                                                               JsonSerializer.SerializeToElement(
                                                                   created.Value,
                                                                   _options.Value.JsonSerializerOptions));
                RedisValue redisValue = JsonSerializer.Serialize(cachedResponse);

                await _cacheDatabase.StringSetAsync(key, redisValue, TimeSpan.FromHours(24), When.NotExists);
            }
        }
        finally
        {
            await _cacheDatabase.KeyDeleteAsync(lockKey);
        }
    }

    private bool GetRedisCacheValue(ActionExecutingContext context, RedisValue redisCacheValue)
    {
        if (!redisCacheValue.HasValue)
        {
            return false;
        }

        CachedCreatedResponse? cachedResponse = JsonSerializer.Deserialize<CachedCreatedResponse>(redisCacheValue!);
        context.Result = new CreatedResult(cachedResponse!.Location, cachedResponse.Body);

        return true;
    }

    private sealed record CachedCreatedResponse(string? Location, JsonElement Body);
}
