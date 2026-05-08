using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Sync entity-level attribute: checks that the total order value
/// (Quantity × UnitPrice) does not exceed a hard maximum.
/// Applied to the class, not to individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class MaxOrderValueAttribute : ValidationAttribute
{
    private readonly decimal _maxValue;

    public MaxOrderValueAttribute(double maxValue)
    {
        _maxValue = (decimal)maxValue;
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;

        var quantity = (int)(type.GetProperty("Quantity")?.GetValue(instance) ?? 0);
        var unitPrice = (decimal)(type.GetProperty("UnitPrice")?.GetValue(instance) ?? 0m);

        decimal total = quantity * unitPrice;
        if (total > _maxValue)
        {
            return new ValidationResult(
                $"Order total ({total:C}) exceeds the maximum allowed value of {_maxValue:C}.",
                new[] { "Quantity", "UnitPrice" });
        }

        return ValidationResult.Success;
    }
}
