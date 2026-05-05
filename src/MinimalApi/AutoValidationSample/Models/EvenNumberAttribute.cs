using System.ComponentModel.DataAnnotations;

namespace AutoValidationSample.Models;

/// <summary>
/// Custom ValidationAttribute that ensures the value is an even integer.
/// Demonstrates that custom attributes work with the automatic validation pipeline.
/// </summary>
public sealed class EvenNumberAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is int number && number % 2 == 0;
    }
}
