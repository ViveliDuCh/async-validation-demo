// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace CrossTypeParallelConsole;

/// <summary>
/// Async validation attribute that simulates checking database connectivity.
/// Adds a 200ms delay to make parallel execution timing visible.
/// </summary>
public class AsyncConnectionReachableAttribute : AsyncValidationAttribute
{
    public AsyncConnectionReachableAttribute()
        : base("Database connection is not reachable.") { }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string connectionString || string.IsNullOrWhiteSpace(connectionString))
            return new ValidationResult("A valid connection string is required.");

        // Simulate async connectivity check (e.g., SqlConnection.OpenAsync)
        await Task.Delay(200, cancellationToken);

        if (connectionString.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Database at '{connectionString}' is not reachable.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
