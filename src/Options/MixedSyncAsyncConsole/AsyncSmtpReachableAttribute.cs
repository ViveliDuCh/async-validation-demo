// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MixedSyncAsyncConsole;

/// <summary>
/// Async validation attribute that simulates checking SMTP host connectivity.
/// Provides BOTH async and sync paths so it works with both
/// ValidateDataAnnotations() (sync) and ValidateDataAnnotationsAsync() (async).
/// 
/// This dual-mode pattern is the recommended approach when mixing sync + async
/// pipelines on the same model — the sync fallback uses Thread.Sleep instead of
/// Task.Delay, allowing sync callers to work without NotSupportedException.
/// </summary>
public class AsyncSmtpReachableAttribute : AsyncValidationAttribute
{
    public AsyncSmtpReachableAttribute()
        : base("SMTP host is not reachable.") { }

    // Async path: used by ValidateDataAnnotationsAsync → TryValidateObjectAsync
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string host || string.IsNullOrWhiteSpace(host))
            return new ValidationResult("A valid SMTP host is required.");

        // Simulate async network probe (e.g., TCP connect on port 25)
        await Task.Delay(150, cancellationToken);

        return ValidateHost(host, validationContext);
    }

    // Sync fallback: used by ValidateDataAnnotations → TryValidateObject
    // Overrides base AsyncValidationAttribute.IsValid (which throws NotSupportedException)
    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        if (value is not string host || string.IsNullOrWhiteSpace(host))
            return new ValidationResult("A valid SMTP host is required.");

        // Sync-over-blocking: simulates sync network probe
        Thread.Sleep(150);

        return ValidateHost(host, validationContext);
    }

    private static ValidationResult? ValidateHost(string host, ValidationContext ctx)
    {
        if (host.Contains("unreachable", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"SMTP host '{host}' is not reachable.",
                new[] { ctx.MemberName! });
        }

        return ValidationResult.Success;
    }
}
