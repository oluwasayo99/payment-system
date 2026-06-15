using Demo.Common.Redis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace Demo.Payments.Service.Filters;


public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IRedisService redis;

    public IdempotencyFilter(IRedisService redis)
    {
        this.redis = redis;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            context.Result = new BadRequestObjectResult(
                new { error = "Idempotency-Key header required" }
            );
            return;
        }

        var idempotencyKey = key.ToString();

        var cached = await redis.GetAsync($"idempotency:{idempotencyKey}");
        if (cached != null)
        {
            context.Result = new OkObjectResult(cached);
            return;
        }

        var lockAcquired = await redis.TryAcquireLockAsync(
            $"lock:{idempotencyKey}",
            TimeSpan.FromSeconds(300)
        );

        if (!lockAcquired)
        {
            context.Result = new ConflictObjectResult(new
            {
                error = "Conflict",
                message = "Request already processing"
            });
            return;
        }

        context.HttpContext.Items["IdempotencyKey"] = idempotencyKey;

        var executedContext = await next();

        if(executedContext.Result is ObjectResult objectResult)
        {
            var value = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            await redis.SetAsync(
                $"idempotency:{idempotencyKey}",
                value,
                TimeSpan.FromHours(24)
            );

            await redis.RemoveAsync($"lock:{idempotencyKey}");
        }
    }
}