using Starter.Domain.Users;

namespace Starter.UnitTests.Domain.Users;

public sealed class EmailAddressRuleTests
{
    private const char LocalPartFillCharacter = 'a';
    private const string DomainSuffix = "@example.com";

    [Theory]
    [InlineData("jane@example.com")]
    [InlineData("  Jane.Doe@Example.COM ")]
    [InlineData("jane.doe+newsletter@mail.example.co.uk")]
    [InlineData("o'neil_j-1@example.org")]
    public void IsValid_WithWellFormedAsciiAddress_ReturnsTrue(string email)
    {
        Assert.True(EmailAddressRule.IsValid(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("x@ ")]
    [InlineData(" @x.com")]
    [InlineData("@example.com")]
    [InlineData("jane@")]
    [InlineData("jane")]
    [InlineData("jane@@example.com")]
    [InlineData("jane@exa@mple.com")]
    [InlineData("jane@localhost")]
    [InlineData("jane@.example.com")]
    [InlineData("jane@example.com.")]
    [InlineData("jane@example..com")]
    [InlineData("jane doe@example.com")]
    [InlineData("😀@example.com")]
    [InlineData("jösé@example.com")]
    [InlineData("jane@münchen.de")]
    [InlineData("jane​@example.com")]
    [InlineData("jane\t@example.com")]
    [InlineData("\u212Aelvin@example.com")]
    [InlineData(".jane@example.com")]
    [InlineData("jane.@example.com")]
    [InlineData("jane..doe@example.com")]
    [InlineData("jane@-example.com")]
    [InlineData("jane@example-.com")]
    [InlineData("<script>@example.com")]
    [InlineData("jane,doe@example.com")]
    [InlineData("jane@exa_mple.com")]
    public void IsValid_WithMalformedOrNonAsciiAddress_ReturnsFalse(string? email)
    {
        Assert.False(EmailAddressRule.IsValid(email));
    }

    [Fact]
    public void IsValid_WithAddressExactlyMaxLength_ReturnsTrue()
    {
        var maxLengthEmail = new string(LocalPartFillCharacter, UserConstraints.EmailMaxLength - DomainSuffix.Length) + DomainSuffix;

        Assert.True(EmailAddressRule.IsValid(maxLengthEmail));
    }

    [Fact]
    public void IsValid_WithAddressLongerThanMaxLength_ReturnsFalse()
    {
        var tooLongEmail = new string(LocalPartFillCharacter, UserConstraints.EmailMaxLength) + DomainSuffix;

        Assert.False(EmailAddressRule.IsValid(tooLongEmail));
    }
}
