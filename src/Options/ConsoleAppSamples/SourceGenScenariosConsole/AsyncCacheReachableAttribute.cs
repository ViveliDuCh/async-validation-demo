// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace SourceGenScenariosConsole;

/// <summary>
/// Async-only validation attribute. IsValid is NOT overridden —
/// base AsyncValidationAttribute.IsValid throws NotSupportedException.
/// </summary>
public class AsyncCacheReachableAttribute : AsyncValidationAttribute
{
    public AsyncCacheReachableAttribute()
        : base("Cache endpoint is not reachable.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException("Use the async validation path.");

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
            return new ValidationResult("A valid cache endpoint is required.");

        ScenarioValidationLog.Log($"Cache async probe ran for Endpoint='{endpoint}'.");
        await Task.Delay(200, cancellationToken);

        if (endpoint.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            return new ValidationResult($"Cache endpoint '{endpoint}' is not reachable.", [validationContext.MemberName!]);

        return ValidationResult.Success;
    }
}
