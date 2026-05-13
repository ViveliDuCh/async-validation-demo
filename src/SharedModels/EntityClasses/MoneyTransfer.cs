// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    /// Async cross-property validation: checks same-account transfer and
    /// simulates an async balance check against an external service.
    /// </summary>
    public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationResult>();

        // Sync cross-property check (no I/O needed)
        if (FromAccount == ToAccount)
        {
            errors.Add(new ValidationResult(
                "Cannot transfer to the same account.",
                new[] { nameof(FromAccount), nameof(ToAccount) }));
        }

        // Async balance check (frees the thread)
        await Task.Delay(50, cancellationToken);
        decimal balance = 500.00m;

        if (Amount > balance)
        {
            errors.Add(new ValidationResult(
                $"Insufficient funds. Balance: ${balance:F2}, Transfer: ${Amount:F2}.",
                new[] { nameof(Amount) }));
        }

        return errors;
    }
}
