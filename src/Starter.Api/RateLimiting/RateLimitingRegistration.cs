using System.Threading.RateLimiting;

namespace Starter.Api.RateLimiting;

internal static class RateLimitingRegistration
{
    private const int RejectInsteadOfQueueing = 0;

    public static IServiceCollection AddClientRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var policies = RateLimitingConfiguration.LoadPolicies(configuration);

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiterOptions.OnRejected = (rejectedContext, _) => RateLimitRejectionHandler.HandleAsync(rejectedContext, policies);
            foreach (var (policyName, policyOptions) in policies)
            {
                limiterOptions.AddPolicy(policyName, httpContext => SlidingWindowPerClient(httpContext, policyOptions));
            }
        });
        return services;
    }

    // Partitioned by client IP while there is no authentication; switch to the user id once auth exists.
    private static RateLimitPartition<string> SlidingWindowPerClient(HttpContext httpContext, SlidingWindowPolicyOptions policyOptions)
        => RateLimitPartition.GetSlidingWindowLimiter(
            ClientPartitionKey.For(httpContext.Connection.RemoteIpAddress),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = policyOptions.PermitLimit,
                Window = policyOptions.Window,
                SegmentsPerWindow = policyOptions.SegmentsPerWindow,
                QueueLimit = RejectInsteadOfQueueing,
            });
}
