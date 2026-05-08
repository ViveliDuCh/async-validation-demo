using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Sync entity-level attribute: checks that the password does not contain the username.
/// Applied to the class, not to individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PasswordPolicyAttribute : ValidationAttribute
{
    private readonly string _usernameProperty;
    private readonly string _passwordProperty;

    public PasswordPolicyAttribute(string usernameProperty, string passwordProperty)
    {
        _usernameProperty = usernameProperty;
        _passwordProperty = passwordProperty;
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;

        var username = type.GetProperty(_usernameProperty)?.GetValue(instance) as string;
        var password = type.GetProperty(_passwordProperty)?.GetValue(instance) as string;

        if (password is not null && username is not null &&
            password.Contains(username, StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                "Password must not contain the username.",
                new[] { _passwordProperty });
        }

        return ValidationResult.Success;
    }
}
