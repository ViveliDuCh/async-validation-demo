// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Tier2.OptionsMonitorBlazor;

/// <summary>
/// Async DataAnnotations validator for <typeparamref name="TOptions"/>.
///
/// On .NET 11+ the in-box <c>DataAnnotationValidateOptions&lt;T&gt;</c> implements
/// both <see cref="IValidateOptions{TOptions}"/> AND
/// <see cref="IAsyncValidateOptions{TOptions}"/> via a partial class. The
/// reference assemblies we build against here pre-date that partial, so we
/// provide an equivalent ourselves: it calls
/// <see cref="Validator.TryValidateObjectAsync(object, ValidationContext, ICollection{ValidationResult}?, bool, CancellationToken)"/>,
/// which honors both sync attributes ([Required], [Range], ...) and
/// <see cref="AsyncValidationAttribute"/> subclasses.
/// </summary>
internal sealed class DataAnnotationsAsyncValidateOptions<TOptions>
    : IAsyncValidateOptions<TOptions>
    where TOptions : class
{
    public string? Name { get; }

    public DataAnnotationsAsyncValidateOptions(string? name) => Name = name;

    public async ValueTask<ValidateOptionsResult> ValidateAsync(
        string? name,
        TOptions options,
        CancellationToken cancellationToken = default)
    {
        // Skip when targeting a different named options instance.
        if (Name is not null && name != Name)
        {
            return ValidateOptionsResult.Skip;
        }

        ArgumentNullException.ThrowIfNull(options);

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        await Validator.TryValidateObjectAsync(
            options, context, results, validateAllProperties: true, cancellationToken);

        if (results.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>(results.Count);
        foreach (ValidationResult r in results)
        {
            string members = r.MemberNames is null
                ? string.Empty
                : string.Join(",", r.MemberNames);
            failures.Add($"DataAnnotation validation failed for '{typeof(TOptions).Name}' members: '{members}' with the error: '{r.ErrorMessage}'.");
        }

        return ValidateOptionsResult.Fail(failures);
    }
}
