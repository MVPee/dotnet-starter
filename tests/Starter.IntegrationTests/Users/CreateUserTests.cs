using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Starter.Domain.Users;
using Starter.Infrastructure.Persistence;
using Starter.IntegrationTests.Contracts;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Users;

[Collection(ApiCollectionDefinition.Name)]
public sealed class CreateUserTests(ApiFactory factory) : IDisposable
{
    private const string FirstName = "Jane";
    private const string LastName = "Doe";
    private const string InvalidEmail = "not-an-email";
    private const string EmailField = "email";
    private const string FirstNameField = "firstName";
    private const string LastNameField = "lastName";
    // Wire values asserted as literals on purpose: they are the public HTTP contract.
    private const string EmailAlreadyUsedCode = "user.email_already_used";
    private const char NameFillCharacter = 'a';
    private const int ConcurrentRequestCount = 10;
    private const string AccentedFirstName = "Zoë";
    private const string HyphenatedLastName = "O'Neil-Müller";

    private static readonly string TooLongName = new(NameFillCharacter, UserConstraints.NameMaxLength + 1);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_WithValidPayload_Returns201WithLocationAndNormalizedUser()
    {
        var email = TestEmails.Unique();

        using var response = await _client.CreateAsync(new CreateUserRequest(email.ToUpperInvariant(), FirstName, LastName));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdUser = await response.ReadAsAsync<UserResponse>();
        Assert.NotEqual(Guid.Empty, createdUser.Id);
        Assert.Equal(email, createdUser.Email);
        Assert.Equal(FirstName, createdUser.FirstName);
        Assert.Equal(LastName, createdUser.LastName);
        Assert.NotEqual(default, createdUser.CreatedAt);

        var location = response.Headers.Location;
        Assert.NotNull(location);
        var locationPath = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
        Assert.Equal($"{UsersApi.BasePath}/{createdUser.Id}", locationPath);
    }

    [Fact]
    public async Task Post_WithValidPayload_PersistsUserWithNormalizedEmail()
    {
        var email = TestEmails.Unique();

        using var response = await _client.CreateAsync(new CreateUserRequest(email.ToUpperInvariant(), FirstName, LastName));
        var createdUser = await response.ReadAsAsync<UserResponse>();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedUser = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(userEntity => userEntity.Id == createdUser.Id);

        Assert.NotNull(persistedUser);
        Assert.Equal(email, persistedUser.Email);
    }

    [Fact]
    public async Task Post_WithEmailAlreadyUsedInDifferentCase_Returns409WithErrorCode()
    {
        var email = TestEmails.Unique();
        using var firstResponse = await _client.CreateAsync(new CreateUserRequest(email, FirstName, LastName));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var response = await _client.CreateAsync(new CreateUserRequest(email.ToUpperInvariant(), FirstName, LastName));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadAsAsync<ProblemResponse>();
        Assert.Equal(EmailAlreadyUsedCode, problem.Code);
    }

    [Fact]
    public async Task Post_WithSameEmailConcurrently_CreatesExactlyOneUserAndReturns409ForOthers()
    {
        var request = new CreateUserRequest(TestEmails.Unique(), FirstName, LastName);

        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentRequestCount)
            .Select(requestIndex => _client.CreateAsync(request)));

        var statusCodes = responses.Select(response => response.StatusCode).ToList();
        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.Single(statusCodes, statusCode => statusCode == HttpStatusCode.Created);
        Assert.All(
            statusCodes.Where(statusCode => statusCode != HttpStatusCode.Created),
            statusCode => Assert.Equal(HttpStatusCode.Conflict, statusCode));
    }

    [Theory]
    [InlineData("x@ ")]
    [InlineData(" @x.com")]
    [InlineData("jane@localhost")]
    [InlineData("😀@example.com")]
    public async Task Post_WithEmailInvalidAfterTrimmingOrNonAscii_Returns400WithEmailError(string email)
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(email, FirstName, LastName));

        await AssertValidationErrorAsync(response, EmailField);
    }

    [Theory]
    [InlineData("Jane 😀")]
    [InlineData("Jane2")]
    [InlineData("Jane\u200BDoe")]
    public async Task Post_WithFirstNameContainingNonNameCharacters_Returns400WithFirstNameError(string firstName)
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), firstName, LastName));

        await AssertValidationErrorAsync(response, FirstNameField);
    }

    [Fact]
    public async Task Post_WithAccentedAndHyphenatedNames_Returns201()
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), AccentedFirstName, HyphenatedLastName));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithInvalidEmailFormat_Returns400WithEmailError()
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(InvalidEmail, FirstName, LastName));

        await AssertValidationErrorAsync(response, EmailField);
    }

    [Fact]
    public async Task Post_WithMissingFirstName_Returns400WithFirstNameError()
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), FirstName: null, LastName));

        await AssertValidationErrorAsync(response, FirstNameField);
    }

    [Fact]
    public async Task Post_WithFirstNameLongerThanMaxLength_Returns400WithFirstNameError()
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), TooLongName, LastName));

        await AssertValidationErrorAsync(response, FirstNameField);
    }

    [Fact]
    public async Task Post_WithLastNameLongerThanMaxLength_Returns400WithLastNameError()
    {
        using var response = await _client.CreateAsync(new CreateUserRequest(TestEmails.Unique(), FirstName, TooLongName));

        await AssertValidationErrorAsync(response, LastNameField);
    }

    public void Dispose() => _client.Dispose();

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string expectedField)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsAsync<ValidationProblemResponse>();
        Assert.Contains(expectedField, problem.Errors.Keys);
    }
}
