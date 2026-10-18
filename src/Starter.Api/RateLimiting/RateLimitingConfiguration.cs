namespace Starter.Api.RateLimiting;

internal static class RateLimitingConfiguration
{
    public const string SectionName = "RateLimiting";
    private const string ReadSectionName = "Read";
    private const string WriteSectionName = "Write";
    private const int MinimumCount = 1;

    private static readonly TimeSpan MinimumWindow = TimeSpan.FromSeconds(1);
    // A bare number binds as days ("60" = 60 days), so a cap catches that typo at startup.
    private static readonly TimeSpan MaximumWindow = TimeSpan.FromDays(1);

    public static IReadOnlyDictionary<string, SlidingWindowPolicyOptions> LoadPolicies(IConfiguration configuration)
    {
        var rateLimitingSection = configuration.GetSection(SectionName);
        return new Dictionary<string, SlidingWindowPolicyOptions>(StringComparer.Ordinal)
        {
            [RateLimitPolicies.Read] = LoadPolicy(rateLimitingSection.GetSection(ReadSectionName)),
            [RateLimitPolicies.Write] = LoadPolicy(rateLimitingSection.GetSection(WriteSectionName)),
        };
    }

    private static SlidingWindowPolicyOptions LoadPolicy(IConfigurationSection policySection)
    {
        var policyOptions = policySection.Get<SlidingWindowPolicyOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{policySection.Path}' is missing.");

        if (policyOptions.PermitLimit < MinimumCount || policyOptions.SegmentsPerWindow < MinimumCount)
        {
            throw new InvalidOperationException($"'{policySection.Path}' needs PermitLimit and SegmentsPerWindow of at least {MinimumCount}.");
        }

        if (policyOptions.Window < MinimumWindow || policyOptions.Window > MaximumWindow)
        {
            throw new InvalidOperationException(
                $"'{policySection.Path}:Window' must be between {MinimumWindow} and {MaximumWindow} (format hh:mm:ss), got {policyOptions.Window}.");
        }

        return policyOptions;
    }
}
