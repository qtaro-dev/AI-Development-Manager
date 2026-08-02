using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Logging;

public sealed class RequestTracingMiddleware(
    RequestDelegate next,
    ILogger<RequestTracingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var candidate = context.Request.Headers[TraceId.HeaderName].FirstOrDefault();
        var traceId = TraceId.GetOrCreate(candidate);
        var stopwatch = Stopwatch.StartNew();

        context.Response.Headers[TraceId.HeaderName] = traceId;
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["trace_id"] = traceId
        });

        logger.RequestStarted(
            context.Request.Method,
            context.Request.Path.Value ?? "/");

        try
        {
            await next(context);
            logger.RequestCompleted(
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.RequestFailed(
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                exception.GetType().FullName ?? exception.GetType().Name);
            throw;
        }
    }
}

public static class RequestTracingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAdmRequestTracing(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestTracingMiddleware>();
}
