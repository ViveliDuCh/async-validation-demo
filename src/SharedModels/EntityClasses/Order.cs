// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using SharedModels.ValidationClasses;

namespace SharedModels.EntityClasses;

/// <summary>
/// API Proposal Scenario 2: IValidatableObject — sync interface with sync + async attributes.
/// [MaxOrderValue] provides a sync hard-limit check ($100K), while Validate()
/// adds a sync cross-property business rule ($50K) simulating an inventory check.
/// </summary>
[MaxOrderValue(100_000)]
[AsyncInventoryCheck]
public class Order : IValidatableObject
{
    /// <summary>Gets or sets the product name.</summary>
    [Required]
    [AsyncProductExists]
    public string? ProductName { get; set; }

    /// <summary>Gets or sets the quantity ordered.</summary>
    [Required]
    [Range(1, 10_000)]
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit price.</summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the simulated I/O delay in milliseconds.</summary>
    [Required]
    public int? Delay { get; set; }

    /// <summary>
    /// Sync inline entity-level validation via IValidatableObject.
    /// Cross-property check: total cost must not exceed the $50,000 business limit.
    /// Simulates a sync inventory check.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Delay is not null)
        {
            Thread.Sleep((int)Delay); // Simulates sync inventory check (blocks thread)
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
