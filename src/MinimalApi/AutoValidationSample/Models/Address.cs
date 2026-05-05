using System.ComponentModel.DataAnnotations;

namespace AutoValidationSample.Models;

/// <summary>
/// Represents a mailing address with validated properties.
/// Validated recursively when nested inside a [ValidatableType] class.
/// </summary>
public class Address
{
    [Required]
    public required string Street { get; set; }

    [Required]
    public required string City { get; set; }

    [StringLength(5)]
    public required string ZipCode { get; set; }
}
