using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Async entity-level attribute: simulates checking an external calendar service
/// for scheduling conflicts. Applied to the class, not to individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsyncScheduleCheckAttribute : AsyncValidationAttribute
{
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;

        var delay = (int?)type.GetProperty("Delay")?.GetValue(instance);
        if (delay is null)
        {
            return new ValidationResult("Delay is not configured.");
        }

        var startDate = (DateTime?)type.GetProperty("StartDate")?.GetValue(instance);

        // Simulate async calendar/schedule conflict check
        await Task.Delay((int)delay, cancellationToken);

        // Simulated rule: events on Jan 1 always conflict (holiday)
        if (startDate is { Month: 1, Day: 1 })
        {
            return new ValidationResult(
                "Schedule conflict: cannot create events on New Year's Day.",
                new[] { "StartDate" });
        }

        return ValidationResult.Success;
    }
}
