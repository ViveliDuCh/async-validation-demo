// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Validation;

namespace MevValidationDemo.Models;

[Microsoft.Extensions.Validation.ValidatableType]
public partial class MevMoneyTransfer : IAsyncValidatableObject
{
    [Required]
    public string? FromAccount { get; set; }

    [Required]
    public string? ToAccount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (FromAccount == ToAccount)
        {
            yield return new ValidationResult(
                "Cannot transfer to the same account.",
                [nameof(FromAccount), nameof(ToAccount)]);
        }

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
