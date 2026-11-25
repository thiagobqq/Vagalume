using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Vagalume.Application.Common;

namespace Vagalume.Api.Observability;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499;
            return true;
        }

        if (exception is BadHttpRequestException badHttp)
        {
            httpContext.Response.StatusCode = badHttp.StatusCode;
            httpContext.Response.ContentType = "application/json";
            var msg = badHttp.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "Arquivo excede o limite de 10 MB"
                : "Requisição inválida";
            await httpContext.Response.WriteAsJsonAsync(GenericResponseData<object>.Fail(msg), cancellationToken);
            return true;
        }

        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(GenericResponseData<object>.Fail("Não autenticado"), cancellationToken);
            return true;
        }

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
