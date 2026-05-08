using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Async entity-level attribute: simulates checking inventory/stock availability
/// for the ordered product and quantity via an external service.
/// Applied to the class, not to individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsyncInventoryCheckAttribute : AsyncValidationAttribute
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

        var productName = type.GetProperty("ProductName")?.GetValue(instance) as string;
        var quantity = (int)(type.GetProperty("Quantity")?.GetValue(instance) ?? 0);

        // Simulate async inventory/stock check
        await Task.Delay((int)delay, cancellationToken);

        // Simulated rule: "Widget" has only 100 units in stock
        if (string.Equals(productName, "Widget", StringComparison.OrdinalIgnoreCase)
            && quantity > 100)
        {
            return new ValidationResult(
                $"Insufficient stock for '{productName}'. Requested: {quantity}, Available: 100.",
                new[] { "Quantity" });
        }

        return ValidationResult.Success;
    }
}
