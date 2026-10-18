using System.Globalization;
using System.Text;

namespace Starter.Domain.Users;

public static class PersonNameRule
{
    public const string Requirement = "must start with a letter and may only contain letters, spaces, hyphens, apostrophes and periods";

    private const char TypographicApostrophe = '’';
    // Real names stack two or three accents at most (Vietnamese); more is "Zalgo" text.
    private const int MaxConsecutiveCombiningMarks = 4;
    private const int FirstVariationSelector = 0xFE00;
    private const int LastVariationSelector = 0xFE0F;
    private const int FirstSupplementaryVariationSelector = 0xE0100;
    private const int LastSupplementaryVariationSelector = 0xE01EF;

    private static readonly HashSet<Rune> AllowedSeparators = [new(' '), new('-'), new('\''), new(TypographicApostrophe), new('.')];

    private static readonly HashSet<UnicodeCategory> LetterCategories =
    [
        UnicodeCategory.UppercaseLetter,
        UnicodeCategory.LowercaseLetter,
        UnicodeCategory.TitlecaseLetter,
        UnicodeCategory.ModifierLetter,
        UnicodeCategory.OtherLetter,
    ];

    private static readonly HashSet<UnicodeCategory> CombiningMarkCategories =
    [
        UnicodeCategory.NonSpacingMark,
        UnicodeCategory.SpacingCombiningMark,
    ];

    // Classified as letters or marks by Unicode yet render as nothing: Hangul fillers, grapheme joiner, Khmer inherent vowels.
    private static readonly HashSet<int> InvisibleCodePoints = [0x115F, 0x1160, 0x3164, 0xFFA0, 0x034F, 0x17B4, 0x17B5];

    public static string Normalize(string name) => name.Trim().Normalize(NormalizationForm.FormC);

    public static bool IsValid(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Runes are checked before Normalize, which throws on an unpaired surrogate; EnumerateRunes turns it into a rejected U+FFFD.
        var trimmedName = name.Trim();
        var hasLetter = false;
        var followsLetterOrMark = false;
        var consecutiveCombiningMarks = 0;
        foreach (var rune in trimmedName.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (IsInvisible(rune))
            {
                return false;
            }

            if (LetterCategories.Contains(category))
            {
                hasLetter = true;
                followsLetterOrMark = true;
                consecutiveCombiningMarks = 0;
            }
            else if (CombiningMarkCategories.Contains(category))
            {
                consecutiveCombiningMarks++;
                if (!followsLetterOrMark || consecutiveCombiningMarks > MaxConsecutiveCombiningMarks)
                {
                    return false;
                }
            }
            else if (hasLetter && AllowedSeparators.Contains(rune))
            {
                followsLetterOrMark = false;
                consecutiveCombiningMarks = 0;
            }
            else
            {
                return false;
            }
        }

        return Normalize(trimmedName).Length <= UserConstraints.NameMaxLength;
    }

    private static bool IsInvisible(Rune rune)
        => InvisibleCodePoints.Contains(rune.Value)
            || rune.Value is >= FirstVariationSelector and <= LastVariationSelector
            || rune.Value is >= FirstSupplementaryVariationSelector and <= LastSupplementaryVariationSelector;
}
