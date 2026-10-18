namespace Starter.IntegrationTests.Fixtures;

internal static class ApiSettingKeys
{
    public const string ReadPermitLimit = "RateLimiting:Read:PermitLimit";
    public const string WritePermitLimit = "RateLimiting:Write:PermitLimit";
    public const string WriteSegmentsPerWindow = "RateLimiting:Write:SegmentsPerWindow";
    public const string ReadWindow = "RateLimiting:Read:Window";
    public const string TrustedNetworks = "ForwardedHeaders:TrustedNetworks";
    public const string FirstTrustedNetwork = "ForwardedHeaders:TrustedNetworks:0";
    public const string AllowedHosts = "AllowedHosts";
}
