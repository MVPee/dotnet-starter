using System.ComponentModel.DataAnnotations;
using Starter.Domain.Users;

namespace Starter.Api.Validation;

// Reuses the Domain rule so the HTTP check and the Domain check can never disagree.
[AttributeUsage(AttributeTargets.Property)]
internal sealed class ValidEmailAttribute() : ValidationAttribute(DefaultErrorMessage)
{
    private const string DefaultErrorMessage = $"The {{0}} field {EmailAddressRule.Requirement}.";

    // Missing values are reported once, by [Required].
    public override bool IsValid(object? value)
        => value is not string email || string.IsNullOrWhiteSpace(email) || EmailAddressRule.IsValid(email);
}
