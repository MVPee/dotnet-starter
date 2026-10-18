namespace Starter.Api.Networking;

internal static class AllowedHostsGuard
{
    public const string AllowedHostsKey = "AllowedHosts";
    private const string AnyHost = "*";
    private const char HostSeparator = ';';

    // "*" accepts any Host header, so a direct request could choose the host written into absolute Location URLs.
    public static void EnsureRestrictedOutsideDevelopment(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var allowedHosts = (configuration[AllowedHostsKey] ?? string.Empty)
            .Split(HostSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (allowedHosts.Length == 0 || allowedHosts.Contains(AnyHost, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"'{AllowedHostsKey}' must list the public host names (e.g. api.example.com;staging-api.example.com) outside Development; '{AnyHost}' is only allowed in Development.");
        }
    }
}
