using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RateLimiter;

/// <summary>
/// Middleware for enforcing request rate limits per client IP or endpoint.
/// </summary>
public static class RateLimiterMiddleware
{
    /// <summary>
    /// Creates the middleware delegate that runs rate-limiting checks
    /// before forwarding the request to the next component in the pipeline.
    /// </summary>
    public static Func<RequestDelegate, RequestDelegate> Create(IRateLimiter core, ILogger? logger = null)
    {
        if (core == null)
        {
            throw new ArgumentNullException(nameof(core));
        }

        return new Func<RequestDelegate, RequestDelegate>(delegate (RequestDelegate next)
        {
            return new RequestDelegate(context => InvokeMiddleware(core, next, context, logger));
        });
    }
    
    private static async Task InvokeMiddleware(IRateLimiter core, RequestDelegate next, HttpContext context, ILogger? logger)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "/";

        logger?.LogDebug("{Method} {Path} @ {Time}",
            context.Request.Method,
            context.Request.Path,
            DateTime.UtcNow.ToString("HH:mm:ss.fff"));

        var allowed = core.Allow(ip, path, DateTime.UtcNow);

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await next(context);
    }
}
