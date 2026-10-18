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
    private const string ValidEmail = "jane@example.com";
    private const string ValidName = "Jane";
    private const string EmailWithBlankDomain = "x@ ";
    private const string NameWithEmoji = "Jane 😀";
    private const string DecomposedAccentName = "Zoé";
    private const string ComposedAccentName = "Zoé";

    [Fact]
    public void Create_WithUntrimmedNames_TrimsNames()
    {
        var user = CreateValid(RawEmail, RawFirstName, RawLastName);

        Assert.Equal(TrimmedFirstName, user.FirstName);
        Assert.Equal(TrimmedLastName, user.LastName);
    }

    [Fact]
    public void Create_WithMixedCaseAndPaddedEmail_NormalizesEmail()
    {
        var user = CreateValid(RawEmail, RawFirstName, RawLastName);

        Assert.Equal(NormalizedEmail, user.Email);
    }

    [Fact]
    public void Create_WithDecomposedAccent_StoresComposedForm()
    {
        var user = CreateValid(ValidEmail, DecomposedAccentName, ValidName);

        Assert.Equal(ComposedAccentName, user.FirstName);
    }

    [Fact]
    public void Create_WithAnyInput_GeneratesVersion7Id()
    {
        var user = CreateValid(RawEmail, RawFirstName, RawLastName);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(ExpectedGuidVersion, user.Id.Version);
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDistinctIds()
    {
        var firstUser = CreateValid(RawEmail, RawFirstName, RawLastName);
        var secondUser = CreateValid(RawEmail, RawFirstName, RawLastName);

        Assert.NotEqual(firstUser.Id, secondUser.Id);
    }

    [Fact]
    public void Create_WithCreationTime_KeepsCreationTime()
    {
        var user = CreateValid(RawEmail, RawFirstName, RawLastName);

        Assert.Equal(TestClock.FixedNow, user.CreatedAt);
    }

    [Fact]
    public void Create_WithEmailBlankAfterTrimming_ReturnsInvalidEmail()
    {
        var result = User.Create(EmailWithBlankDomain, ValidName, ValidName, TestClock.FixedNow);

        Assert.Equal(UserErrors.InvalidEmail, result.Error);
    }

    [Fact]
    public void Create_WithInvalidFirstName_ReturnsInvalidFirstName()
    {
        var result = User.Create(ValidEmail, NameWithEmoji, ValidName, TestClock.FixedNow);

        Assert.Equal(UserErrors.InvalidFirstName, result.Error);
    }

    [Fact]
    public void Create_WithInvalidLastName_ReturnsInvalidLastName()
    {
        var result = User.Create(ValidEmail, ValidName, NameWithEmoji, TestClock.FixedNow);

        Assert.Equal(UserErrors.InvalidLastName, result.Error);
    }

    private static User CreateValid(string email, string firstName, string lastName)
    {
        var result = User.Create(email, firstName, lastName, TestClock.FixedNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
