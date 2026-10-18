namespace Starter.IntegrationTests.Contracts;

internal sealed record ProblemResponse(int Status, string? Title, string? Code);
