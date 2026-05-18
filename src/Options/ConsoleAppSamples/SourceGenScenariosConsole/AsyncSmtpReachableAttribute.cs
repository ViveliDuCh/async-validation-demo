// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace SourceGenScenariosConsole;

/// <summary>
/// Dual-mode async validation attribute: provides both IsValidAsync (async pipeline)
/// and IsValid (sync fallback) so it works with both ValidateAsync() and Validate().
/// </summary>
public class AsyncSmtpReachableAttribute : AsyncValidationAttribute
{
    public AsyncSmtpReachableAttribute()
        : base("SMTP host is not reachable.") { }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string host || string.IsNullOrWhiteSpace(host))
            return new ValidationResult("A valid SMTP host is required.");

        ScenarioValidationLog.Log($"SMTP async probe ran for Host='{host}'.");
        await Task.Delay(150, cancellationToken);
        return ValidateHost(host, validationContext);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string host || string.IsNullOrWhiteSpace(host))
            return new ValidationResult("A valid SMTP host is required.");

        ScenarioValidationLog.Log($"SMTP sync fallback ran for Host='{host}'.");
        Thread.Sleep(150);
        return ValidateHost(host, validationContext);
    }

    private static ValidationResult? ValidateHost(string host, ValidationContext ctx)
    {
        if (host.Contains("unreachable", StringComparison.OrdinalIgnoreCase))
            return new ValidationResult($"SMTP host '{host}' is not reachable.", [ctx.MemberName!]);
        return ValidationResult.Success;
    }
}
