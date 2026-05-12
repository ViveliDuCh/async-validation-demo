using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;
using SharedModels.ValidationClasses;

namespace MevValidationDemo.Models;

/// <summary>
/// Local model mirroring SharedModels.Order but with [ValidatableType]
/// to activate the MEV source-gen code path in DataAnnotationsValidator.
/// Reuses the same async validation attributes from SharedModels.
/// </summary>
[Microsoft.Extensions.Validation.ValidatableType]
[MaxOrderValue(100_000)]
[AsyncInventoryCheck]
public partial class MevOrder : IAsyncValidatableObject
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

    public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();

        if (Delay is null)
        {
            results.Add(new ValidationResult("Delay is not configured."));
            return results;
        }

        await Task.Delay((int)Delay, cancellationToken);

        decimal totalCost = Quantity * UnitPrice;
        if (totalCost > 50_000m)
        {
            results.Add(new ValidationResult(
                $"Total cost ({totalCost:C}) exceeds the $50,000 business limit.",
                new[] { nameof(Quantity), nameof(UnitPrice) }));
        }

        return results;
    }
}
