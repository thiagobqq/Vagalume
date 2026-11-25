using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Vagalume.Application.Common;

namespace Vagalume.Api.Observability;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Exceção não tratada em {Method} {Path} (TraceId: {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var envelope = GenericResponseData<object>.Fail($"Erro interno inesperado. TraceId: {traceId}");
        await httpContext.Response.WriteAsJsonAsync(envelope, cancellationToken);
        return true;
    }
}
