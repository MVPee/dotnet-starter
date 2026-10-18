namespace Starter.IntegrationTests.Contracts;

internal sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName, DateTimeOffset CreatedAt);
