// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// API Proposal Scenario 1: Async property attribute that checks username availability.
/// Simulates a database round-trip to check uniqueness.
/// Matches the UsernameAvailableAsync example from dotnet/runtime#128096.
/// Replaces AsyncEmailDomainAttribute per Jeff's feedback — "more intuitive example."
/// </summary>
public class UsernameAvailableAsyncAttribute : AsyncValidationAttribute
{
    public UsernameAvailableAsyncAttribute()
        : base("The username is already taken.") { }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string username || string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Success; // Let [Required] handle nulls
        }

        // Simulates a database round-trip to check uniqueness
        await Task.Delay(200, cancellationToken);
        bool isTaken = username.Equals("admin", StringComparison.OrdinalIgnoreCase);

        return isTaken
            ? new ValidationResult(
                  $"The username '{username}' is already taken.",
                  new[] { validationContext.MemberName! })
            : ValidationResult.Success;
    }
}
