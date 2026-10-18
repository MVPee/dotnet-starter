using System.ComponentModel.DataAnnotations;
using Starter.Domain.Users;

namespace Starter.Api.Validation;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class ValidPersonNameAttribute() : ValidationAttribute(DefaultErrorMessage)
{
    private const string DefaultErrorMessage = $"The {{0}} field {PersonNameRule.Requirement}.";

    public override bool IsValid(object? value)
        => value is not string name || string.IsNullOrWhiteSpace(name) || PersonNameRule.IsValid(name);
}
