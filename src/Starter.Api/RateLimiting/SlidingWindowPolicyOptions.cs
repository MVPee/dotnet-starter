namespace Starter.Api.RateLimiting;

internal sealed class SlidingWindowPolicyOptions
{
    public int PermitLimit { get; init; }

    public TimeSpan Window { get; init; }

    public int SegmentsPerWindow { get; init; }
}
