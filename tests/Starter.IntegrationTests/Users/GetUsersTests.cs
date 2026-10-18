using System.Net;
using Starter.IntegrationTests.Contracts;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Users;

[Collection(ApiCollectionDefinition.Name)]
public sealed class GetUsersTests(ApiFactory factory) : IDisposable
{
    private const string FirstName = "Grace";
    private const string LastName = "Hopper";
    private const string PageSizeField = "pageSize";
    private const int RequestedPageSize = 1;
    private const int InvalidPageSize = 0;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithEmailFilterInDifferentCase_ReturnsOnlyThatUser()
    {
        var email = TestEmails.Unique();
        using var createResponse = await _client.CreateAsync(new CreateUserRequest(email, FirstName, LastName));

        using var response = await _client.ListAsync($"email={Uri.EscapeDataString(email.ToUpperInvariant())}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedUsers = await response.ReadAsAsync<PagedResponse<UserResponse>>();
        Assert.Equal(email, Assert.Single(pagedUsers.Items).Email);
        Assert.Equal(1, pagedUsers.TotalCount);
    }

    [Fact]
    public async Task Get_WithPageSize_ReturnsAtMostThatManyUsers()
    {
        using var firstCreateResponse = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), FirstName, LastName));
        using var secondCreateResponse = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), FirstName, LastName));

        using var response = await _client.ListAsync($"pageSize={RequestedPageSize}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedUsers = await response.ReadAsAsync<PagedResponse<UserResponse>>();
        Assert.Single(pagedUsers.Items);
        Assert.Equal(RequestedPageSize, pagedUsers.PageSize);
        Assert.True(pagedUsers.TotalCount > RequestedPageSize);
    }

    [Fact]
    public async Task Get_WithInvalidPageSize_Returns400WithPageSizeError()
    {
        using var response = await _client.ListAsync($"pageSize={InvalidPageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsAsync<ValidationProblemResponse>();
        Assert.Contains(problem.Errors.Keys, errorKey => string.Equals(errorKey, PageSizeField, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose() => _client.Dispose();
}
