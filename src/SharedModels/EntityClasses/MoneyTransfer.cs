// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.EntityClasses;

/// <summary>
/// API Proposal Scenario 3: IAsyncValidatableObject — async cross-property validation.
/// Matches the MoneyTransfer entity from dotnet/runtime#128096.
/// </summary>
public class MoneyTransfer : IAsyncValidatableObject
{
    [Required]
    public string? FromAccount { get; set; }

    [Required]
    public string? ToAccount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Sync validation is not supported — use ValidateAsync instead.
    /// </summary>
    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        => throw new InvalidOperationException("Use the async validation path (ValidateAsync).");

    /// <summary>
    /// Async cross-property validation: checks same-account transfer and
    /// simulates an async balance check against an external service.
    /// </summary>
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Sync cross-property check (no I/O needed)
        if (FromAccount == ToAccount)
        {
            yield return new ValidationResult(
                "Cannot transfer to the same account.",
                [nameof(FromAccount), nameof(ToAccount)]);
        }

        // Async balance check (frees the thread)
        await Task.Delay(50, cancellationToken);
        decimal balance = 500.00m;

        if (Amount > balance)
        {
            yield return new ValidationResult(
                $"Insufficient funds. Balance: ${balance:F2}, Transfer: ${Amount:F2}.",
                [nameof(Amount)]);
        }
    }
}
