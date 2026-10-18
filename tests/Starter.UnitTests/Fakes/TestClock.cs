namespace Starter.UnitTests.Fakes;

internal static class TestClock
{
    public static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
}
