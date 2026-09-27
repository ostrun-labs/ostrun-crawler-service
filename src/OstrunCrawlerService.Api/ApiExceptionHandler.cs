using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OstrunCrawlerService.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Malformed JSON or a body that doesn't bind surfaces as BadHttpRequestException
        // (thrown in Development); it is the caller's fault, not a server error.
        var (statusCode, title) = exception is BadHttpRequestException badRequest
            ? (badRequest.StatusCode, "The request body is invalid.")
            : (StatusCodes.Status500InternalServerError, "An unexpected error occurred.");

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
        }, cancellationToken);

        return true;
    }
}
