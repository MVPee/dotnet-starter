using NetArchTest.Rules;

namespace Starter.ArchitectureTests;

internal static class TestResultAssertions
{
    public static void AssertRule(this TestResult result, string rule)
    {
        var offendingTypes = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful, $"{rule}. Offending types: {string.Join(", ", offendingTypes)}");
    }

    public static void AssertNone(this IEnumerable<Type> types, string rule)
    {
        var offendingTypes = types.Select(type => type.FullName).ToList();
        Assert.True(offendingTypes.Count == 0, $"{rule}. Offending types: {string.Join(", ", offendingTypes)}");
    }
}
