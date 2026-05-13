// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace SharedModels.ValidationClasses;

/// <summary>
/// API Proposal Scenario 1: Sync property attribute.
/// Simulates a synchronous I/O call (Thread.Sleep) to validate a name.
/// Matches the IsValidName example from dotnet/runtime#128096.
/// </summary>
public class IsValidNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Success; // Let [Required] handle nulls
        }

        // Simulates sync I/O (blocks thread)
        Thread.Sleep(50);

        return ValidationResult.Success;
    }
}
