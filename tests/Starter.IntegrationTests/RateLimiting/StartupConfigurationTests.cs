using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.RateLimiting;

[Collection(ApiCollectionDefinition.Name)]
public sealed class StartupConfigurationTests(ApiFactory factory)
{
    private const string NoValue = "";
    private const string MalformedNetwork = "not-a-cidr";
    private const string ProxyNetwork = "10.0.1.0/24";
    private const string ZeroSegments = "0";
    private const string WindowWrittenAsBareNumber = "60";
    private const string RateLimitingSectionName = "RateLimiting";
    private const string AnyHost = "*";
    private const string PublicHost = "api.example.com";
    private const string UnknownHost = "evil.example";
    private const string PublicHostThenAnyHost = "api.example.com; * ";

    private static readonly Uri HealthPath = new("/health", UriKind.Relative);

    [Fact]
    public void Startup_OutsideDevelopmentWithoutTrustedNetworks_Fails()
    {
        using var misconfiguredFactory = factory.WithWebHostBuilder(builder => builder
            .UseEnvironment(Environments.Production)
            .UseSetting(ApiSettingKeys.AllowedHosts, PublicHost));

        AssertStartupFailsMentioning(misconfiguredFactory, ApiSettingKeys.TrustedNetworks);
    }

    [Theory]
    [InlineData(AnyHost)]
    [InlineData(NoValue)]
    [InlineData(PublicHostThenAnyHost)]
    public void Startup_OutsideDevelopmentWithUnrestrictedHosts_Fails(string allowedHosts)
    {
        using var misconfiguredFactory = factory.WithWebHostBuilder(builder => builder
            .UseEnvironment(Environments.Production)
            .UseSetting(ApiSettingKeys.AllowedHosts, allowedHosts)
            .UseSetting(ApiSettingKeys.FirstTrustedNetwork, ProxyNetwork));

        AssertStartupFailsMentioning(misconfiguredFactory, ApiSettingKeys.AllowedHosts);
    }

    [Fact]
    public async Task Production_WithRestrictedHosts_ServesKnownHostAndRejectsOthers()
    {
        await using var productionFactory = factory.WithWebHostBuilder(builder => builder
            .UseEnvironment(Environments.Production)
            .UseSetting(ApiSettingKeys.AllowedHosts, PublicHost)
            .UseSetting(ApiSettingKeys.FirstTrustedNetwork, ProxyNetwork));
        using var client = productionFactory.CreateClient();

        using var knownHostResponse = await GetHealthAsync(client, PublicHost);
        using var unknownHostResponse = await GetHealthAsync(client, UnknownHost);

        Assert.Equal(HttpStatusCode.OK, knownHostResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownHostResponse.StatusCode);
    }

    private static Task<HttpResponseMessage> GetHealthAsync(HttpClient client, string host)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
        request.Headers.Host = host;
        return client.SendAsync(request);
    }

    [Fact]
    public void Startup_WithMalformedTrustedNetwork_FailsNamingTheEntry()
    {
        using var misconfiguredFactory = factory.WithWebHostBuilder(builder => builder
            .UseSetting(ApiSettingKeys.FirstTrustedNetwork, MalformedNetwork));

        AssertStartupFailsMentioning(misconfiguredFactory, MalformedNetwork);
    }

    [Fact]
    public void Startup_WithZeroSegmentsPerWindow_Fails()
    {
        using var misconfiguredFactory = factory.WithWebHostBuilder(builder => builder
            .UseSetting(ApiSettingKeys.WriteSegmentsPerWindow, ZeroSegments));

        AssertStartupFailsMentioning(misconfiguredFactory, RateLimitingSectionName);
    }

    [Fact]
    public void Startup_WithWindowWrittenAsBareNumber_Fails()
    {
        using var misconfiguredFactory = factory.WithWebHostBuilder(builder => builder
            .UseSetting(ApiSettingKeys.ReadWindow, WindowWrittenAsBareNumber));

        AssertStartupFailsMentioning(misconfiguredFactory, ApiSettingKeys.ReadWindow);
    }

    private static void AssertStartupFailsMentioning(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> misconfiguredFactory, string expectedMessagePart)
    {
        var startupFailure = Assert.Throws<InvalidOperationException>(() => misconfiguredFactory.CreateClient());
        Assert.Contains(expectedMessagePart, startupFailure.Message, StringComparison.Ordinal);
    }
}
