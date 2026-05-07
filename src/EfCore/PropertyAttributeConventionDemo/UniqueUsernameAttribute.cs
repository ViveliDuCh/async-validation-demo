using System.ComponentModel.DataAnnotations;

namespace PropertyAttributeConventionDemo;

/// <summary>
/// Async validation attribute that checks username uniqueness.
/// Extends the real AsyncValidationAttribute from the prototype runtime DLL.
///
/// EF Core never calls IsValidAsync() — it only detects the attribute's
/// presence via reflection. The convention decides what schema action to take.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UniqueUsernameAttribute : AsyncValidationAttribute
{
    public UniqueUsernameAttribute()
    {
        ErrorMessage = "This username is already taken.";
    }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        // EF Core convention testing: this method is never called.
        // EF Core only uses reflection to detect the attribute type.
        await Task.CompletedTask;
        return ValidationResult.Success;
    }
}
