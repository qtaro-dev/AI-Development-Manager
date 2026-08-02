using System.Text.Json;
using Adm.Application.Errors;
using Adm.Server.Host.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Adm.Server.Host.Errors;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            if (context.Response.StatusCode >= 400 &&
                !context.Response.HasStarted &&
                context.Response.ContentLength is null &&
                string.IsNullOrEmpty(context.Response.ContentType))
            {
                await WriteAsync(context, CreateDescriptor(context.Response.StatusCode), null);
            }
        }
        catch (AdmApplicationException exception) when (!context.Response.HasStarted)
        {
            var descriptor = AdmErrorCatalog.From(exception);
            logger.ClassifiedError(exception, descriptor.Code);
            await WriteAsync(context, descriptor, exception);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var descriptor = AdmErrorCatalog.FromUnknown();
            logger.UnhandledError(exception);
            await WriteAsync(context, descriptor, exception);
        }
    }

    private static AdmErrorDescriptor CreateDescriptor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => AdmErrorCatalog.From(new AdmValidationException()),
        StatusCodes.Status403Forbidden => AdmErrorCatalog.From(new AdmForbiddenException()),
        StatusCodes.Status404NotFound => AdmErrorCatalog.From(new AdmNotFoundException()),
        StatusCodes.Status409Conflict => AdmErrorCatalog.From(new AdmConflictException()),
        _ => AdmErrorCatalog.FromUnknown()
    };

    private static async Task WriteAsync(
        HttpContext context,
        AdmErrorDescriptor descriptor,
        Exception? exception)
    {
        var traceId = context.Response.Headers[TraceId.HeaderName].FirstOrDefault()
            ?? TraceId.GetOrCreate(null);
        context.Response.Clear();
        context.Response.StatusCode = descriptor.Status;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers[TraceId.HeaderName] = traceId;
        var problem = new AdmProblemDetails(
            $"/problems/{descriptor.Code}",
            descriptor.UserMessage,
            descriptor.Status,
            descriptor.UserMessage,
            context.Request.Path.Value ?? "/",
            descriptor.Code,
            descriptor.MessageKey,
            descriptor.InputRetained,
            descriptor.Retryable,
            descriptor.NextAction,
            traceId);

        await JsonSerializer.SerializeAsync(context.Response.Body, problem);
    }
}

public static class ErrorHandlingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAdmErrorHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ErrorHandlingMiddleware>();
}
