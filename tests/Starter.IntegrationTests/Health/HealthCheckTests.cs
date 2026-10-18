using System.Net;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Health;

[Collection(ApiCollectionDefinition.Name)]
public sealed class HealthCheckTests(ApiFactory factory) : IDisposable
{
    private static readonly Uri HealthPath = new("/health", UriKind.Relative);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithDatabaseUp_Returns200()
    {
        using var response = await _client.GetAsync(HealthPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _client.Dispose();
}
