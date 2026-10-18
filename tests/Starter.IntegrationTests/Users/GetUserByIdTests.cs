using System.Net;
using Starter.IntegrationTests.Contracts;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Users;

[Collection(ApiCollectionDefinition.Name)]
public sealed class GetUserByIdTests(ApiFactory factory) : IDisposable
{
    private const string FirstName = "John";
    private const string LastName = "Smith";
    private const string NotFoundCode = "user.not_found";

    // Postgres truncates .NET's 100ns ticks to microseconds.
    private static readonly TimeSpan CreatedAtPrecision = TimeSpan.FromSeconds(1);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_AfterCreatingUser_Returns200WithSameBody()
    {
        using var createResponse = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), FirstName, LastName));
        var createdUser = await createResponse.ReadAsAsync<UserResponse>();

        using var response = await _client.GetByIdAsync(createdUser.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetchedUser = await response.ReadAsAsync<UserResponse>();
        Assert.Equal(createdUser with { CreatedAt = fetchedUser.CreatedAt }, fetchedUser);
        Assert.Equal(createdUser.CreatedAt, fetchedUser.CreatedAt, CreatedAtPrecision);
    }

    [Fact]
    public async Task Get_WithUnknownId_Returns404WithErrorCode()
    {
        using var response = await _client.GetByIdAsync(Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadAsAsync<ProblemResponse>();
        Assert.Equal(NotFoundCode, problem.Code);
    }

    public void Dispose() => _client.Dispose();
}
