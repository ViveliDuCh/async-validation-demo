// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace TransitiveValidationConsole;

/// <summary>
/// Async validation attribute that simulates checking endpoint health.
/// Used on collection items via <c>[ValidateEnumeratedItems]</c> to demonstrate
/// that <c>ValidateDataAnnotationsAsync</c> recursively validates each item
/// in annotated collections.
/// </summary>
public class AsyncEndpointHealthyAttribute : AsyncValidationAttribute
{
    public AsyncEndpointHealthyAttribute()
        : base("Endpoint is unhealthy.") { }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
            return new ValidationResult("Endpoint URL is required.");

        // Simulate async health check (e.g., HttpClient.GetAsync)
        await Task.Delay(50, cancellationToken);

        if (endpoint.Contains("unhealthy", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Endpoint '{endpoint}' health check failed.",
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
            return new ValidationResult("Endpoint URL is required.");

        Thread.Sleep(50);

        if (endpoint.Contains("unhealthy", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Endpoint '{endpoint}' health check failed.",
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
