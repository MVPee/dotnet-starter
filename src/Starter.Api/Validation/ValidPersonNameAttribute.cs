using System.ComponentModel.DataAnnotations;
using Starter.Domain.Users;

namespace Starter.Api.Validation;

// Reuses the Domain rule so the HTTP check and the Domain check can never disagree.
[AttributeUsage(AttributeTargets.Property)]
internal sealed class ValidPersonNameAttribute() : ValidationAttribute(DefaultErrorMessage)
{
    private const string DefaultErrorMessage = $"The {{0}} field {PersonNameRule.Requirement}.";

    // Missing values are reported once, by [Required].
    public override bool IsValid(object? value)
        => value is not string name || string.IsNullOrWhiteSpace(name) || PersonNameRule.IsValid(name);
}
