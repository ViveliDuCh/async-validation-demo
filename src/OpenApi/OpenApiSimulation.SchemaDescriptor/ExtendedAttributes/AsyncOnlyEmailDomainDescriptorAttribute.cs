// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

/// <summary>
/// Copy of AsyncOnlyEmailDomainAttribute that adds ISchemaDescriptor.
/// Demonstrates an async attribute with constructor parameters that
/// produce RICHER schema metadata via ISchemaDescriptor.
/// </summary>
public class AsyncOnlyEmailDomainDescriptorAttribute : AsyncValidationAttribute, ISchemaDescriptor
{
    private readonly string _requiredDomain;

    public AsyncOnlyEmailDomainDescriptorAttribute(string requiredDomain)
        : base($"Email must belong to the '{requiredDomain}' domain.")
    {
        _requiredDomain = requiredDomain;
    }

    // ISchemaDescriptor — exposes constructor parameter as schema metadata
    public string? SchemaDescription => $"Email must belong to the '{_requiredDomain}' domain";
    public string? SchemaFormat => "email";
    public string? SchemaPattern => $@"@{Regex.Escape(_requiredDomain)}$";
    public int? SchemaMaxLength => null;
    public int? SchemaMinLength => null;
    public IReadOnlyDictionary<string, object>? SchemaExtensions =>
        new Dictionary<string, object>
        {
            ["x-async-validation"] = true,
            ["x-required-domain"] = _requiredDomain
        };

    // Simplified validation (no User-specific cast or Delay dependency)
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string email)
            return new ValidationResult("A valid email string is required.");

        await Task.Delay(10, cancellationToken); // Simulate async DNS lookup

        if (!email.EndsWith($"@{_requiredDomain}", StringComparison.OrdinalIgnoreCase))
            return new ValidationResult($"'{email}' is not in the '{_requiredDomain}' domain.");

        return ValidationResult.Success;
    }
}
