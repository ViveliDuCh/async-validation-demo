using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Async property attribute with sync-over-async fallback (Case 4).
/// Checks if a title is reserved by querying a simulated external service.
///
/// When called via <c>Validator.TryValidateObjectAsync</c>, the async path runs
/// natively. When called via <c>Validator.TryValidateObject</c> (sync), the
/// <see cref="IsValid"/> override calls <see cref="IsValidAsync"/> synchronously
/// via <c>Task.Result</c>, blocking the thread — demonstrating the sync-over-async
/// fallback pattern.
/// </summary>
public class ReservedTitleCheckAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> s_reservedTitles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Test Event",
            "Default",
            "Placeholder"
        };

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string title || string.IsNullOrEmpty(title))
        {
            return ValidationResult.Success;
        }

        var delay = (int?)validationContext.ObjectType
            .GetProperty("Delay")?
            .GetValue(validationContext.ObjectInstance);

        if (delay is null)
        {
            return new ValidationResult("Delay is not configured.");
        }

        // Simulate async lookup against a reserved-titles service
        await Task.Delay((int)delay, cancellationToken);

        if (s_reservedTitles.Contains(title))
        {
            return new ValidationResult(
                $"The title '{title}' is reserved and cannot be used.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Sync-over-async fallback: calls <see cref="IsValidAsync"/> synchronously
    /// via <c>Task.Result</c>. This blocks the calling thread but allows sync
    /// callers (<c>Validator.TryValidateObject</c>) to work with this attribute.
    /// Async callers should use <c>Validator.TryValidateObjectAsync</c> instead.
    /// </summary>
    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        return IsValidAsync(value, validationContext, CancellationToken.None)
            .AsTask()
            .GetAwaiter().GetResult();
    }
}
