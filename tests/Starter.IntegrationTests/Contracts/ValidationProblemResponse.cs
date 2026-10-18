namespace Starter.IntegrationTests.Contracts;

internal sealed record ValidationProblemResponse(int Status, Dictionary<string, string[]> Errors);
