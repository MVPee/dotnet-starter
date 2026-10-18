using Starter.Domain.Common;

namespace Starter.Domain.Users;

public static class UserErrors
{
    public const string EmailAlreadyUsedCode = "user.email_already_used";
    public const string NotFoundCode = "user.not_found";

    public static readonly Error EmailAlreadyUsed =
        Error.Conflict(EmailAlreadyUsedCode, "A user with this email already exists.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound(NotFoundCode, $"User '{userId}' was not found.");
}
