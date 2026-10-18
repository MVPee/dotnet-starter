namespace Starter.IntegrationTests.Contracts;

internal sealed record CreateUserRequest(string? Email, string? FirstName, string? LastName);
