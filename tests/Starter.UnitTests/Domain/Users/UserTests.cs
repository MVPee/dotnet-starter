using Starter.Domain.Users;
using Starter.UnitTests.Fakes;

namespace Starter.UnitTests.Domain.Users;

public sealed class UserTests
{
    private const int ExpectedGuidVersion = 7;
    private const string RawEmail = "  John.Doe@Example.COM ";
    private const string NormalizedEmail = "john.doe@example.com";
    private const string RawFirstName = "  John ";
    private const string RawLastName = " Doe  ";
    private const string TrimmedFirstName = "John";
    private const string TrimmedLastName = "Doe";

    [Fact]
    public void Create_WithUntrimmedNames_TrimsNames()
    {
        var user = User.Create(RawEmail, RawFirstName, RawLastName, TestClock.FixedNow);

        Assert.Equal(TrimmedFirstName, user.FirstName);
        Assert.Equal(TrimmedLastName, user.LastName);
    }

    [Fact]
    public void Create_WithMixedCaseAndPaddedEmail_NormalizesEmail()
    {
        var user = User.Create(RawEmail, RawFirstName, RawLastName, TestClock.FixedNow);

        Assert.Equal(NormalizedEmail, user.Email);
    }

    [Fact]
    public void Create_WithAnyInput_GeneratesVersion7Id()
    {
        var user = User.Create(RawEmail, RawFirstName, RawLastName, TestClock.FixedNow);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(ExpectedGuidVersion, user.Id.Version);
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDistinctIds()
    {
        var firstUser = User.Create(RawEmail, RawFirstName, RawLastName, TestClock.FixedNow);
        var secondUser = User.Create(RawEmail, RawFirstName, RawLastName, TestClock.FixedNow);

        Assert.NotEqual(firstUser.Id, secondUser.Id);
    }

    [Fact]
    public void Create_WithCreationTime_KeepsCreationTime()
    {
        var user = User.Create(RawEmail, RawFirstName, RawLastName, TestClock.FixedNow);

        Assert.Equal(TestClock.FixedNow, user.CreatedAt);
    }
}
