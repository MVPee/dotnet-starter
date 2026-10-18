using Starter.Domain.Users;

namespace Starter.UnitTests.Domain.Users;

public sealed class PersonNameRuleTests
{
    private const char NameFillCharacter = 'a';
    private const string UnpairedSurrogate = "Jane\uD800";

    [Theory]
    [InlineData("Jane")]
    [InlineData("  Jane ")]
    [InlineData("Jean-Pierre")]
    [InlineData("O'Neil")]
    [InlineData("O’Neil")]
    [InlineData("St. John")]
    [InlineData("Zoë Müller")]
    [InlineData("Zoé")]
    [InlineData("李小龙")]
    [InlineData("Ελένη")]
    [InlineData("Nguye\u0302\u0303n")]
    [InlineData("Jr.")]
    public void IsValid_WithLettersAndNameSeparators_ReturnsTrue(string name)
    {
        Assert.True(PersonNameRule.IsValid(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData("' .")]
    [InlineData("Jane 😀")]
    [InlineData("👍")]
    [InlineData("Jane2")]
    [InlineData("Jane_Doe")]
    [InlineData("Jane@Doe")]
    [InlineData("Jane​Doe")]
    [InlineData("Jane\tDoe")]
    [InlineData("<script>")]
    [InlineData("\u3164")]
    [InlineData("Jane\u3164")]
    [InlineData("A\uFE0F")]
    [InlineData("\u0301Jane")]
    [InlineData("a\u0301\u0301\u0301\u0301\u0301")]
    [InlineData("-Jane")]
    [InlineData("'Jane")]
    [InlineData("Ja\u200Cne")]
    [InlineData("\u202EJane")]
    public void IsValid_WithDigitsSymbolsEmojiOrInvisibleCharacters_ReturnsFalse(string? name)
    {
        Assert.False(PersonNameRule.IsValid(name));
    }

    [Fact]
    public void IsValid_WithUnpairedSurrogate_ReturnsFalseInsteadOfThrowing()
    {
        Assert.False(PersonNameRule.IsValid(UnpairedSurrogate));
    }

    [Fact]
    public void IsValid_WithNameLongerThanMaxLength_ReturnsFalse()
    {
        var tooLongName = new string(NameFillCharacter, UserConstraints.NameMaxLength + 1);

        Assert.False(PersonNameRule.IsValid(tooLongName));
    }
}
