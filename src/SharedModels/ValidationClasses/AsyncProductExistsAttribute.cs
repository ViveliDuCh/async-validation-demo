using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Async property attribute: checks that a product name exists in the catalog
/// by simulating an async database lookup.
/// </summary>
public class AsyncProductExistsAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> s_validProducts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Widget", "Gadget", "Gizmo", "Doohickey", "Thingamajig"
        };

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string productName || string.IsNullOrEmpty(productName))
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

        // Simulate async product catalog lookup
        await Task.Delay((int)delay, cancellationToken);

        if (!s_validProducts.Contains(productName))
        {
            return new ValidationResult(
                $"Product '{productName}' does not exist in the catalog.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
