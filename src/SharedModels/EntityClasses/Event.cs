// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SharedModels.ValidationClasses;

namespace SharedModels.EntityClasses;

/// <summary>
/// Case 2: IValidatableObject — sync interface with sync + async attributes.
/// [ReservedTitleCheck] demonstrates the sync-over-async fallback pattern (Case 4).
/// IValidatableObject.Validate() provides additional sync entity-level inline logic.
/// </summary>
[DateRange(nameof(StartDate), nameof(EndDate))]
[AsyncScheduleCheck]
public class Event : IValidatableObject
{
    /// <summary>Gets or sets the event title.</summary>
    [Required]
    [StringLength(200)]
    [ReservedTitleCheck]
    public string? Title { get; set; }

    /// <summary>Gets or sets the event start date.</summary>
    [Required]
    public DateTime? StartDate { get; set; }

    /// <summary>Gets or sets the event end date.</summary>
    [Required]
    public DateTime? EndDate { get; set; }

    /// <summary>Gets or sets the simulated I/O delay in milliseconds.</summary>
    [Required]
    public int? Delay { get; set; }

    /// <summary>
    /// Sync inline entity-level validation via IValidatableObject.
    /// Rejects "TBD" titles when dates are confirmed.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title?.Contains("TBD", StringComparison.OrdinalIgnoreCase) == true
            && StartDate.HasValue && EndDate.HasValue)
        {
            yield return new ValidationResult(
                "Events with confirmed dates cannot have 'TBD' in the title.",
                new[] { nameof(Title) });
        }
    }
}
