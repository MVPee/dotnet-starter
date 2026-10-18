using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.RateLimiting;

// TestServer has no network peer, so a startup filter plays the TCP peer that Traefik (or an attacker) would be.
[Collection(ApiCollectionDefinition.Name)]
public sealed class ClientIpRateLimitingTests(ApiFactory factory)
{
    private const string SimulatedPeerHeader = "X-Test-Peer-Ip";
    private const string ForwardedForHeader = "X-Forwarded-For";
    private const string UsersPath = "/api/users";
    private const string SinglePermit = "1";
    private const string TraefikPeer = "10.0.1.5";
    private const string UntrustedPeer = "198.51.100.7";
    private const string FirstClient = "203.0.113.9";
    private const string SecondClient = "203.0.113.10";
    private const string SpoofedClient = "203.0.113.20";
    private const string OtherSpoofedClient = "203.0.113.21";
    private const string Ipv6Client = "2001:db8:1:2::1";
    private const string Ipv6ClientSameNetwork = "2001:db8:1:2::ffff";
    private const string Ipv6ClientOtherNetwork = "2001:db8:1:3::1";
    private const string Ipv4Client = "203.0.113.30";
    private const string Ipv4MappedClient = "::ffff:203.0.113.30";

    [Fact]
    public async Task TrustedProxy_ForwardingDifferentClients_GivesEachItsOwnQuota()
    {
        await using var limitedFactory = WithSimulatedPeers();
        using var client = limitedFactory.CreateClient();

        using var firstResponse = await GetUsersAsync(client, TraefikPeer, FirstClient);
        using var secondResponse = await GetUsersAsync(client, TraefikPeer, SecondClient);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UntrustedPeer_SpoofingForwardedFor_StaysInItsOwnQuota()
    {
        await using var limitedFactory = WithSimulatedPeers();
        using var client = limitedFactory.CreateClient();

        using var firstResponse = await GetUsersAsync(client, UntrustedPeer, SpoofedClient);
        using var spoofedResponse = await GetUsersAsync(client, UntrustedPeer, OtherSpoofedClient);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, spoofedResponse.StatusCode);
    }

    [Fact]
    public async Task Ipv6Clients_InTheSame64Network_ShareOneQuota()
    {
        await using var limitedFactory = WithSimulatedPeers();
        using var client = limitedFactory.CreateClient();

        using var firstResponse = await GetUsersAsync(client, TraefikPeer, Ipv6Client);
        using var sameNetworkResponse = await GetUsersAsync(client, TraefikPeer, Ipv6ClientSameNetwork);
        using var otherNetworkResponse = await GetUsersAsync(client, TraefikPeer, Ipv6ClientOtherNetwork);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, sameNetworkResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherNetworkResponse.StatusCode);
    }

    [Fact]
    public async Task Ipv4MappedAddress_SharesTheQuotaOfItsIpv4Address()
    {
        await using var limitedFactory = WithSimulatedPeers();
        using var client = limitedFactory.CreateClient();

        using var mappedResponse = await GetUsersAsync(client, TraefikPeer, Ipv4MappedClient);
        using var ipv4Response = await GetUsersAsync(client, TraefikPeer, Ipv4Client);

        Assert.Equal(HttpStatusCode.OK, mappedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, ipv4Response.StatusCode);
    }

    private WebApplicationFactory<Program> WithSimulatedPeers()
        => factory.WithWebHostBuilder(builder => builder
            .UseSetting(ApiSettingKeys.ReadPermitLimit, SinglePermit)
            .ConfigureServices(services => services.AddSingleton<IStartupFilter, SimulatedPeerStartupFilter>()));

    private static Task<HttpResponseMessage> GetUsersAsync(HttpClient client, string peerIp, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(UsersPath, UriKind.Relative));
        request.Headers.Add(SimulatedPeerHeader, peerIp);
        request.Headers.Add(ForwardedForHeader, forwardedFor);
        return client.SendAsync(request);
    }

    private sealed class SimulatedPeerStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => applicationBuilder =>
        {
            applicationBuilder.Use((httpContext, nextMiddleware) =>
            {
                if (httpContext.Request.Headers.TryGetValue(SimulatedPeerHeader, out var peerIp))
                {
                    httpContext.Connection.RemoteIpAddress = IPAddress.Parse(peerIp.ToString());
                }

                return nextMiddleware(httpContext);
            });
            next(applicationBuilder);
        };
    }
}
