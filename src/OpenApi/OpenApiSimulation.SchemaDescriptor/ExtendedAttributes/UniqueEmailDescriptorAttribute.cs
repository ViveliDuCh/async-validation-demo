// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.ServiceClasses;

/// <summary>
/// Copy of UniqueEmailAttribute that adds ISchemaDescriptor.
/// </summary>
public class UniqueEmailDescriptorAttribute : AsyncValidationAttribute, ISchemaDescriptor
{
    public string? SchemaDescription => "Email must be unique (validated server-side)";
    public string? SchemaFormat => "email";
    public string? SchemaPattern => null;
    public int? SchemaMaxLength => null;
    public int? SchemaMinLength => null;
    public IReadOnlyDictionary<string, object>? SchemaExtensions =>
        new Dictionary<string, object>
        {
            ["x-async-validation"] = true,
            ["x-requires-server-check"] = true,
            ["x-async-validation-type"] = "unique-email",
            ["x-uniqueness-check"] = "email"
        };

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string email || string.IsNullOrEmpty(email))
            return ValidationResult.Success;

        var userService = (UserService?)validationContext.GetService(typeof(UserService));
        if (userService is null)
            return new ValidationResult("UserService is not available.");

        bool taken = await userService.IsEmailTakenAsync(email, cancellationToken);
        return taken
            ? new ValidationResult($"The email '{email}' is already registered.")
            : ValidationResult.Success;
    }
}
