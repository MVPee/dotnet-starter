using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Starter.Api.Errors;

internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment hostEnvironment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string UnexpectedErrorTitle = "An unexpected error occurred.";
    private const string BadRequestTitle = "The request could not be processed.";
    // Non-standard but widely used (nginx) to mark requests the client abandoned.
    private const int ClientClosedRequestStatusCode = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequestStatusCode;
            return true;
        }

        if (exception is BadHttpRequestException badHttpRequestException)
        {
            LogBadRequest(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
            return await WriteProblemAsync(httpContext, exception, badHttpRequestException.StatusCode, BadRequestTitle, detail: null);
        }

        LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        var detail = hostEnvironment.IsDevelopment() ? exception.ToString() : null;
        return await WriteProblemAsync(httpContext, exception, StatusCodes.Status500InternalServerError, UnexpectedErrorTitle, detail);
    }

    private async ValueTask<bool> WriteProblemAsync(HttpContext httpContext, Exception exception, int statusCode, string title, string? detail)
    {
        httpContext.Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title, Detail = detail },
            Exception = exception,
        });
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {RequestMethod} {RequestPath}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string requestMethod, string requestPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected malformed request {RequestMethod} {RequestPath}")]
    private static partial void LogBadRequest(ILogger logger, Exception exception, string requestMethod, string requestPath);
}
