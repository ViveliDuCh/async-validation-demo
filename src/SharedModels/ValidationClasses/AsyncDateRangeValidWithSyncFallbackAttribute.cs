// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// API Proposal Scenario 4: Async attribute with sync fallback.
/// Overrides both IsValidAsync (async path) and IsValid (sync path)
/// so that sync callers (Validator.TryValidateObject) can still use this attribute
/// without throwing NotSupportedException.
/// Matches the AsyncDateRangeValidWithSyncFallback example from dotnet/runtime#128096.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsyncDateRangeValidWithSyncFallbackAttribute : AsyncValidationAttribute
{
    private readonly string _startProp;
    private readonly string _endProp;

    public AsyncDateRangeValidWithSyncFallbackAttribute(string startProp, string endProp)
    {
        _startProp = startProp;
        _endProp = endProp;
    }

    // Async path: used by TryValidateObjectAsync (non-blocking)
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulates async calendar check
        return ValidateDateRange(validationContext);
    }

    // Sync fallback: used by TryValidateObject (blocks the thread)
    // Overrides the base AsyncValidationAttribute.IsValid which throws NotSupportedException
    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        // Sync-over-async: blocks the calling thread
        // Intentional for backward compat with sync-only callers
        Thread.Sleep(50); // Simulates sync calendar check
        return ValidateDateRange(validationContext);
    }

    // Shared validation logic (no I/O)
    private ValidationResult? ValidateDateRange(ValidationContext validationContext)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var start = (DateTime?)type.GetProperty(_startProp)?.GetValue(instance);
        var end = (DateTime?)type.GetProperty(_endProp)?.GetValue(instance);

        return start.HasValue && end.HasValue && start.Value >= end.Value
            ? new ValidationResult(
                  $"'{_startProp}' must be before '{_endProp}'.",
                  new[] { _startProp, _endProp })
            : ValidationResult.Success;
    }
}
