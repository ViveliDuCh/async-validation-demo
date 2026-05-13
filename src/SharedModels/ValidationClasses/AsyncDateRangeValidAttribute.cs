// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SharedModels.ValidationClasses;

/// <summary>
/// API Proposal Scenario 1: Async entity-level attribute applied to the class.
/// Simulates calling a calendar/scheduling service to get the maximum allowed date,
/// then validates both start &lt; end and end &lt;= maxDateAllowed.
/// Matches the AsyncDateRangeValid example from dotnet/runtime#128096.
/// Per Jeff's feedback: "simulate getting a value back from the service like maxDateAllowed."
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsyncDateRangeValidAttribute : AsyncValidationAttribute
{
    private readonly string _startProp;
    private readonly string _endProp;

    public AsyncDateRangeValidAttribute(string startProp, string endProp)
    {
        _startProp = startProp;
        _endProp = endProp;
    }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        // Simulates calling a calendar/scheduling service to get max allowed date
        await Task.Delay(5000, cancellationToken);
        DateTime maxDateAllowed = DateTime.UtcNow.AddYears(1); // Service response

        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var start = (DateTime?)type.GetProperty(_startProp)?.GetValue(instance);
        var end = (DateTime?)type.GetProperty(_endProp)?.GetValue(instance);

        if (start.HasValue && end.HasValue && start.Value >= end.Value)
        {
            return new ValidationResult(
                $"'{_startProp}' must be before '{_endProp}'.",
                new[] { _startProp, _endProp });
        }

        if (end.HasValue && end.Value > maxDateAllowed)
        {
            return new ValidationResult(
                $"'{_endProp}' cannot be later than {maxDateAllowed:d} (service limit).",
                new[] { _endProp });
        }

        return ValidationResult.Success;
    }
}
