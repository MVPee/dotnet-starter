using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Starter.IntegrationTests.Contracts;
using Starter.IntegrationTests.Fixtures;
using Starter.IntegrationTests.Users;

namespace Starter.IntegrationTests.RateLimiting;

[Collection(ApiCollectionDefinition.Name)]
public sealed class RateLimitingTests(ApiFactory factory)
{
    private const string ProblemJsonMediaType = "application/problem+json";
    // Wire value asserted as a literal on purpose: it is the public HTTP contract.
    private const string RateLimitExceededCode = "rate_limit.exceeded";
    private const string FirstName = "Ada";
    private const string LastName = "Lovelace";
    private const int LowPermitLimit = 2;
    private const int SinglePermit = 1;
    private const int HighPermitLimit = 1000;
    private const int HealthProbeCount = 5;

    // Matches the one-minute window in appsettings.json: only a full window guarantees permits are back.
    private static readonly TimeSpan ExpectedRetryAfter = TimeSpan.FromMinutes(1);

    private static readonly Uri HealthPath = new("/health", UriKind.Relative);

    [Fact]
    public async Task Post_BeyondWriteLimit_Returns429ProblemWithRetryAfter()
    {
        await using var limitedFactory = WithLimits(readPermitLimit: HighPermitLimit, writePermitLimit: LowPermitLimit);
        using var client = limitedFactory.CreateClient();

        for (var allowedRequest = 0; allowedRequest < LowPermitLimit; allowedRequest++)
        {
            using var allowedResponse = await client.CreateAsync(NewUserRequest());
            Assert.Equal(HttpStatusCode.Created, allowedResponse.StatusCode);
        }

        using var rejectedResponse = await client.CreateAsync(NewUserRequest());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.Equal(ProblemJsonMediaType, rejectedResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ExpectedRetryAfter, rejectedResponse.Headers.RetryAfter?.Delta);
        var problem = await rejectedResponse.ReadAsAsync<ProblemResponse>();
        Assert.Equal(RateLimitExceededCode, problem.Code);
    }

    [Fact]
    public async Task Get_BeyondReadLimit_Returns429()
    {
        await using var limitedFactory = WithLimits(readPermitLimit: SinglePermit, writePermitLimit: HighPermitLimit);
        using var client = limitedFactory.CreateClient();

        using var allowedResponse = await client.ListAsync(string.Empty);
        using var rejectedResponse = await client.ListAsync(string.Empty);

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
    }

    [Fact]
    public async Task Writes_DoNotConsumeReadQuota()
    {
        await using var limitedFactory = WithLimits(readPermitLimit: SinglePermit, writePermitLimit: HighPermitLimit);
        using var client = limitedFactory.CreateClient();

        using var createResponse = await client.CreateAsync(NewUserRequest());
        using var readResponse = await client.ListAsync(string.Empty);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
    }

    [Fact]
    public async Task Health_IsNeverRateLimited()
    {
        await using var limitedFactory = WithLimits(readPermitLimit: SinglePermit, writePermitLimit: SinglePermit);
        using var client = limitedFactory.CreateClient();

        for (var probe = 0; probe < HealthProbeCount; probe++)
        {
            using var response = await client.GetAsync(HealthPath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private WebApplicationFactory<Program> WithLimits(int readPermitLimit, int writePermitLimit)
        => factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(ApiSettingKeys.ReadPermitLimit, readPermitLimit.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting(ApiSettingKeys.WritePermitLimit, writePermitLimit.ToString(CultureInfo.InvariantCulture));
        });

    private static CreateUserRequest NewUserRequest() => new(TestEmails.Unique(), FirstName, LastName);
}
