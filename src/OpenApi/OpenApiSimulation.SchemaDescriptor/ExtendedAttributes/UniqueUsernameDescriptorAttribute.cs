// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.ServiceClasses;

/// <summary>
/// Copy of UniqueUsernameAttribute that adds ISchemaDescriptor.
/// Shows what the attribute would look like if it opted into schema visibility.
/// </summary>
public class UniqueUsernameDescriptorAttribute : AsyncValidationAttribute, ISchemaDescriptor
{
    // ISchemaDescriptor
    public string? SchemaDescription => "Username must be unique (validated server-side)";
    public string? SchemaFormat => null;
    public string? SchemaPattern => null;
    public int? SchemaMaxLength => null;
    public int? SchemaMinLength => null;
    public IReadOnlyDictionary<string, object>? SchemaExtensions =>
        new Dictionary<string, object>
        {
            ["x-async-validation"] = true,
            ["x-requires-server-check"] = true,
            ["x-async-validation-type"] = "unique-username",
            ["x-uniqueness-check"] = "username"
        };

    // Validation logic (identical to original)
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string username || string.IsNullOrEmpty(username))
            return ValidationResult.Success;

        var userService = (UserService?)validationContext.GetService(typeof(UserService));
        if (userService is null)
            return new ValidationResult("UserService is not available.");

        bool taken = await userService.IsUsernameTakenAsync(username, cancellationToken);
        return taken
            ? new ValidationResult($"The username '{username}' is already taken.")
            : ValidationResult.Success;
    }
}
