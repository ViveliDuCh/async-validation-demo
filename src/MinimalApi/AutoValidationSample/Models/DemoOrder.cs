using System.ComponentModel.DataAnnotations;

namespace AutoValidationSample.Models;

/// <summary>
/// Order model using IValidatableObject for custom validation logic.
/// Named DemoOrder to avoid collision with SharedModels.EntityClasses.Order.
/// </summary>
public class DemoOrder : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Required]
    public required string ProductName { get; set; }

    public int Quantity { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity <= 0)
        {
            yield return new ValidationResult(
                "Quantity must be greater than zero",
                [nameof(Quantity)]);
        }
    }
}
