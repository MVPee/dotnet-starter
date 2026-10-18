using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Starter.Api.Errors;

namespace Starter.Api.RateLimiting;

internal static partial class RateLimitRejectionHandler
{
    public const string ErrorCode = "rate_limit.exceeded";
    private const string ProblemTitle = "Too many requests.";
    private const string ProblemDetail = "The request limit for this client was exceeded. Retry after the delay given in the Retry-After header.";
    private const string ProblemJsonContentType = "application/problem+json";

    public static async ValueTask HandleAsync(OnRejectedContext context, IReadOnlyDictionary<string, SlidingWindowPolicyOptions> policies)
    {
        var httpContext = context.HttpContext;
        var policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        // Sliding window leases carry no retry hint, and only a full window guarantees the oldest permits are back.
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var leaseRetryAfter)
            ? leaseRetryAfter
            : policies[policyName ?? throw new InvalidOperationException("A rate limit rejection always comes from a named policy.")].Window;
        httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitRejectionHandler).FullName!);
        LogRejected(logger, policyName, httpContext.Connection.RemoteIpAddress?.ToString(), httpContext.Request.Method, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = ProblemTitle,
            Detail = ProblemDetail,
            Extensions = { [ErrorProblemDetailsExtensions.ErrorCodeExtensionKey] = ErrorCode },
        };
        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        // No problem writer accepts e.g. "Accept: text/html"; still answer with the JSON contract instead of an empty body.
        if (!await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problemDetails }))
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: ProblemJsonContentType);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit '{PolicyName}' exceeded by {ClientIp} on {RequestMethod} {RequestPath}")]
    private static partial void LogRejected(ILogger logger, string? policyName, string? clientIp, string requestMethod, string requestPath);
}
