// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SharedModels.ValidationClasses;

namespace SharedModels.EntityClasses;

/// <summary>
/// API Proposal Scenarios 1 + 4: IValidatableObject with async entity-level attribute.
/// [AsyncDateRangeValid] simulates a calendar service call to get maxDateAllowed,
/// then validates both start &lt; end and end &lt;= maxDateAllowed.
/// IValidatableObject.Validate() provides additional sync entity-level inline logic.
/// </summary>
[AsyncDateRangeValid(nameof(StartDate), nameof(EndDate))]
public class Event : IValidatableObject
{
    /// <summary>Gets or sets the event title.</summary>
    [Required]
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>Gets or sets the event start date.</summary>
    [Required]
    public DateTime? StartDate { get; set; }

    /// <summary>Gets or sets the event end date.</summary>
    [Required]
    public DateTime? EndDate { get; set; }

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
