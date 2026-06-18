// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace TransitiveValidationConsole;

/// <summary>
/// Async validation attribute that simulates checking database connectivity.
/// Used on nested <see cref="DatabaseSettings"/> to demonstrate that
/// <c>ValidateDataAnnotationsAsync</c> recursively validates nested objects
/// marked with <c>[ValidateObjectMembers]</c>.
/// </summary>
public class AsyncConnectionStringValidAttribute : AsyncValidationAttribute
{
    public AsyncConnectionStringValidAttribute()
        : base("Connection string is invalid or unreachable.") { }

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string connStr || string.IsNullOrWhiteSpace(connStr))
            return new ValidationResult("Connection string is required.");

        // Simulate async DB connectivity check (e.g., SqlConnection.OpenAsync)
        await Task.Delay(100, cancellationToken);

        if (connStr.Contains("unreachable", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Database at '{connStr}' is not reachable.",
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        if (value is not string connStr || string.IsNullOrWhiteSpace(connStr))
            return new ValidationResult("Connection string is required.");

        Thread.Sleep(100);

        if (connStr.Contains("unreachable", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Database at '{connStr}' is not reachable.",
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
