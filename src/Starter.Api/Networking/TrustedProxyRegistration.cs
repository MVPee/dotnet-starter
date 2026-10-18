using Microsoft.AspNetCore.HttpOverrides;

namespace Starter.Api.Networking;

internal static class TrustedProxyRegistration
{
    public const string TrustedNetworksKey = "ForwardedHeaders:TrustedNetworks";

    public static IServiceCollection AddTrustedProxyForwardedHeaders(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Blank entries count as unset, so clearing the value through an environment variable trips the guard below.
        var trustedNetworks = (configuration.GetSection(TrustedNetworksKey).Get<string[]>() ?? [])
            .Where(trustedNetwork => !string.IsNullOrWhiteSpace(trustedNetwork))
            .Select(ParseTrustedNetwork)
            .ToList();
        // Without the proxy network every client looks like the proxy, so IP rate limiting would throttle everyone together.
        if (trustedNetworks.Count == 0 && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"'{TrustedNetworksKey}' must list the reverse proxy network (the dokploy-network subnet) outside Development.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // An empty known-network list makes the middleware trust every peer, so forwarding stays off when none is configured.
            options.ForwardedHeaders = trustedNetworks.Count == 0
                ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var trustedNetwork in trustedNetworks)
            {
                options.KnownIPNetworks.Add(trustedNetwork);
            }
        });
        return services;
    }

    private static System.Net.IPNetwork ParseTrustedNetwork(string trustedNetwork)
        => System.Net.IPNetwork.TryParse(trustedNetwork, out var parsedNetwork)
            ? parsedNetwork
            : throw new InvalidOperationException($"'{TrustedNetworksKey}' entry '{trustedNetwork}' is not a CIDR network such as 10.0.1.0/24.");
}
