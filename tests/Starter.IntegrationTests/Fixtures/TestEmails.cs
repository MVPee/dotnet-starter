namespace Starter.IntegrationTests.Fixtures;

internal static class TestEmails
{
    public static string Unique() => $"user-{Guid.NewGuid():N}@example.com";
}
