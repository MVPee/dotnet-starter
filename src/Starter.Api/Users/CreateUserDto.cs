using System.ComponentModel.DataAnnotations;
using Starter.Api.Validation;

namespace Starter.Api.Users;

// Plain init properties (not positional, not `required`) so every invalid field is reported under its camelCase name.
// Lengths are enforced by the Domain rules behind [ValidEmail]/[ValidPersonName], on the trimmed value.
public sealed record CreateUserDto
{
    [Required, ValidEmail]
    public string Email { get; init; } = string.Empty;

    [Required, ValidPersonName]
    public string FirstName { get; init; } = string.Empty;

    [Required, ValidPersonName]
    public string LastName { get; init; } = string.Empty;
}
