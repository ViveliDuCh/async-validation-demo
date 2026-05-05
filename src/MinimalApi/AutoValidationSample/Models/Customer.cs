using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace AutoValidationSample.Models;

/// <summary>
/// Represents a customer with validated properties and a nested <see cref="Address"/>.
/// <see cref="ValidatableTypeAttribute"/> tells the source generator to generate
/// validation logic at compile time, including recursive nested object validation.
/// </summary>
[ValidatableType]
public class Customer
{
    [Required]
    public required string Name { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    [Range(18, 120)]
    [Display(Name = "Customer Age")]
    public int Age { get; set; }

    /// <summary>Nested object — validated recursively thanks to [ValidatableType].</summary>
    public Address HomeAddress { get; set; } = new()
    {
        Street = "123 Main St",
        City = "Anytown",
        ZipCode = "12345"
    };
}
