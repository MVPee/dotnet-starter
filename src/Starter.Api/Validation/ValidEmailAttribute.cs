using System.ComponentModel.DataAnnotations;
using Starter.Domain.Users;

namespace Starter.Api.Validation;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class ValidEmailAttribute() : ValidationAttribute(DefaultErrorMessage)
{
    private const string DefaultErrorMessage = $"The {{0}} field {EmailAddressRule.Requirement}.";

    public override bool IsValid(object? value)
        => value is not string email || string.IsNullOrWhiteSpace(email) || EmailAddressRule.IsValid(email);
}
