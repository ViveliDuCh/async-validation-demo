using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Async entity-level attribute: simulates an async screening check against
/// a blocklist/fraud-detection service during registration.
/// Applied to the class, not to individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsyncRegistrationScreenAttribute : AsyncValidationAttribute
{
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var email = type.GetProperty("Email")?.GetValue(instance) as string;

        // Simulate async blocklist/fraud screening service call
        await Task.Delay(50, cancellationToken);

        if (email is not null &&
            email.EndsWith("@blocked.com", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                "Registration blocked: email domain is not allowed.",
                new[] { "Email" });
        }

        return ValidationResult.Success;
    }
}
