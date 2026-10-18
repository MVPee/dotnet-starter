using System.Net;
using System.Net.Sockets;

namespace Starter.Api.RateLimiting;

internal static class ClientPartitionKey
{
    public const string UnknownClient = "unknown-client";
    private const int Ipv6NetworkPrefixBytes = 8;
    private const string Ipv6NetworkPrefixSuffix = "/64";

    // A client usually owns a whole IPv6 /64; keying on the full address would let it rotate addresses to reset its quota.
    public static string For(IPAddress? remoteIpAddress)
    {
        if (remoteIpAddress is null)
        {
            return UnknownClient;
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            return remoteIpAddress.MapToIPv4().ToString();
        }

        if (remoteIpAddress.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return remoteIpAddress.ToString();
        }

        var addressBytes = remoteIpAddress.GetAddressBytes();
        Array.Clear(addressBytes, Ipv6NetworkPrefixBytes, addressBytes.Length - Ipv6NetworkPrefixBytes);
        return new IPAddress(addressBytes) + Ipv6NetworkPrefixSuffix;
    }
}
