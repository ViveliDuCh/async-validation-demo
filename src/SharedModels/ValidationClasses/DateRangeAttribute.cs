using System;
using System.ComponentModel.DataAnnotations;

namespace SharedModels.ValidationClasses;

/// <summary>
/// Sync entity-level attribute: validates that a start date occurs before an end date.
/// Applied to the class, not to individual properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DateRangeAttribute : ValidationAttribute
{
    private readonly string _startProperty;
    private readonly string _endProperty;

    public DateRangeAttribute(string startProperty, string endProperty)
    {
        _startProperty = startProperty;
        _endProperty = endProperty;
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;

        var start = (DateTime?)type.GetProperty(_startProperty)?.GetValue(instance);
        var end = (DateTime?)type.GetProperty(_endProperty)?.GetValue(instance);

        if (start.HasValue && end.HasValue && start.Value >= end.Value)
        {
            return new ValidationResult(
                $"'{_startProperty}' must be before '{_endProperty}'.",
                new[] { _startProperty, _endProperty });
        }

        return ValidationResult.Success;
    }
}
