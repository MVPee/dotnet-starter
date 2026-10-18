using System.ComponentModel.DataAnnotations;
using Starter.Domain.Users;

namespace Starter.Api.Users;

// Plain init properties (not positional, not `required`) so every invalid field is reported under its camelCase name.
public sealed record CreateUserDto
{
    [Required, EmailAddress, MaxLength(UserConstraints.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(UserConstraints.NameMaxLength)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(UserConstraints.NameMaxLength)]
    public string LastName { get; init; } = string.Empty;
}
