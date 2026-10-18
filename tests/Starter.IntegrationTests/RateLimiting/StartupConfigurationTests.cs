using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.RateLimiting;

[Collection(ApiCollectionDefinition.Name)]
public sealed class StartupConfigurationTests(ApiFactory factory)
{
    private const string NoValue = "";
    private const string MalformedNetwork = "not-a-cidr";
    private const string ZeroSegments = "0";
    private const string WindowWrittenAsBareNumber = "60";
    private const string RateLimitingSectionName = "RateLimiting";

    [Fact]
    public void Startup_OutsideDevelopmentWithoutTrustedNetworks_Fails()
    {
        using var misconfiguredFactory = factory.WithWebHostBuilder(builder => builder
            .UseEnvironment(Environments.Production)
            .UseSetting(ApiSettingKeys.FirstTrustedNetwork, NoValue));

        AssertStartupFailsMentioning(misconfiguredFactory, ApiSettingKeys.TrustedNetworks);
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
