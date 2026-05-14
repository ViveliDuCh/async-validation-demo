// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace CrossTypeParallelConsole;

/// <summary>
/// Async validation attribute that simulates checking cache endpoint connectivity.
/// Adds a 200ms delay to make parallel execution timing visible.
/// </summary>
public class AsyncCacheReachableAttribute : AsyncValidationAttribute
{
    public AsyncCacheReachableAttribute()
        : base("Cache endpoint is not reachable.") { }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
            return new ValidationResult("A valid cache endpoint is required.");

        // Simulate async connectivity check (e.g., Redis PING)
        await Task.Delay(200, cancellationToken);

        if (endpoint.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Cache endpoint '{endpoint}' is not reachable.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
