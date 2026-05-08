using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Copy of PasswordPolicyAttribute that adds ISchemaDescriptor.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PasswordPolicyDescriptorAttribute : ValidationAttribute, ISchemaDescriptor
{
    private readonly string _usernameProperty;
    private readonly string _passwordProperty;

    public PasswordPolicyDescriptorAttribute(string usernameProperty, string passwordProperty)
    {
        _usernameProperty = usernameProperty;
        _passwordProperty = passwordProperty;
    }

    public string? SchemaDescription => "Password must not contain the username.";
    public string? SchemaFormat => null;
    public string? SchemaPattern => null;
    public int? SchemaMaxLength => null;
    public int? SchemaMinLength => null;
    public IReadOnlyDictionary<string, object>? SchemaExtensions =>
        new Dictionary<string, object>
        {
            ["x-password-policy"] = "username-exclusion",
            ["x-validation-scope"] = "entity"
        };

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
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
