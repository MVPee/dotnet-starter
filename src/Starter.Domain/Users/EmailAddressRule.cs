namespace Starter.Domain.Users;

public static class EmailAddressRule
{
    public const string Requirement = "must be a valid ASCII email address such as name@example.com";

    private const char AtSign = '@';
    private const char DotSeparator = '.';
    private const char Hyphen = '-';
    private const int MinimumDomainLabelCount = 2;
    private const string LocalPartSymbols = "!#$%&'*+/=?^_`{|}~-";

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        // Checked before lowercasing: ToLowerInvariant maps some non-ASCII characters (the Kelvin sign) to ASCII letters.
        var trimmedEmail = email.Trim();
        var atSignIndex = trimmedEmail.IndexOf(AtSign, StringComparison.Ordinal);
        if (trimmedEmail.Length > UserConstraints.EmailMaxLength
            || atSignIndex <= 0
            || atSignIndex != trimmedEmail.LastIndexOf(AtSign))
        {
            return false;
        }

        return IsValidLocalPart(trimmedEmail[..atSignIndex]) && IsValidDomain(trimmedEmail[(atSignIndex + 1)..]);
    }

    private static bool IsValidLocalPart(string localPart)
        => localPart.Split(DotSeparator).All(localPartAtom =>
            localPartAtom.Length > 0 && localPartAtom.All(character => char.IsAsciiLetterOrDigit(character) || LocalPartSymbols.Contains(character)));

    private static bool IsValidDomain(string domain)
    {
        var domainLabels = domain.Split(DotSeparator);
        return domainLabels.Length >= MinimumDomainLabelCount && domainLabels.All(IsValidDomainLabel);
    }

    private static bool IsValidDomainLabel(string domainLabel)
        => domainLabel.Length > 0
            && domainLabel[0] != Hyphen
            && domainLabel[^1] != Hyphen
            && domainLabel.All(character => char.IsAsciiLetterOrDigit(character) || character == Hyphen);
}
