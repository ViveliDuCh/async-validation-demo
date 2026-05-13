using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;
using SharedModels.ValidationClasses;

namespace MevValidationDemo.Models;

/// <summary>
/// Local model mirroring SharedModels.Order but with [ValidatableType]
/// to activate the MEV source-gen code path in DataAnnotationsValidator.
/// Reuses the same async validation attributes from SharedModels.
/// Now uses IValidatableObject (Scenario 2) to match the updated Order entity.
/// </summary>
[Microsoft.Extensions.Validation.ValidatableType]
[MaxOrderValue(100_000)]
[AsyncInventoryCheck]
public partial class MevOrder : IValidatableObject
{
    [Required]
    [AsyncProductExists]
    public string? ProductName { get; set; }

    [Required]
    [Range(1, 10_000)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Required]
    public int? Delay { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Delay is not null)
        {
            Thread.Sleep((int)Delay); // Simulates sync inventory check
        }

        decimal totalCost = Quantity * UnitPrice;
        if (totalCost > 50_000m)
        {
            yield return new ValidationResult(
                $"Total cost ({totalCost:C}) exceeds the $50,000 limit.",
                new[] { nameof(Quantity), nameof(UnitPrice) });
        }
    }
}
