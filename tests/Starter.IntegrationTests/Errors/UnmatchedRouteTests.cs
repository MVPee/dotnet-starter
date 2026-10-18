using System.Net;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Errors;

[Collection(ApiCollectionDefinition.Name)]
public sealed class UnmatchedRouteTests(ApiFactory factory) : IDisposable
{
    private const string ProblemJsonMediaType = "application/problem+json";
    private static readonly Uri NonGuidUserPath = new("/api/users/not-a-guid", UriKind.Relative);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithRouteThatMatchesNoEndpoint_Returns404ProblemDetails()
    {
        using var response = await _client.GetAsync(NonGuidUserPath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
    }

    public void Dispose() => _client.Dispose();
}
