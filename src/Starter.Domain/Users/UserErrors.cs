using Starter.Domain.Common;

namespace Starter.Domain.Users;

public static class UserErrors
{
    public const string EmailAlreadyUsedCode = "user.email_already_used";
    public const string NotFoundCode = "user.not_found";
    public const string InvalidEmailCode = "user.invalid_email";
    public const string InvalidFirstNameCode = "user.invalid_first_name";
    public const string InvalidLastNameCode = "user.invalid_last_name";

    public static readonly Error EmailAlreadyUsed =
        Error.Conflict(EmailAlreadyUsedCode, "A user with this email already exists.");

    public static readonly Error InvalidEmail =
        Error.Validation(InvalidEmailCode, $"The email {EmailAddressRule.Requirement}.");

    public static readonly Error InvalidFirstName =
        Error.Validation(InvalidFirstNameCode, $"The first name {PersonNameRule.Requirement}.");

    public static readonly Error InvalidLastName =
        Error.Validation(InvalidLastNameCode, $"The last name {PersonNameRule.Requirement}.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound(NotFoundCode, $"User '{userId}' was not found.");
}
