// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.ValidationClasses;

namespace SharedModels.EntityClasses;

/// <summary>
/// Case 1: No interface — validation is entirely attribute-driven.
/// Demonstrates sync + async property-level and entity-level attributes.
/// DI-backed async property attrs resolve UserService via ValidationContext.GetService().
/// </summary>
[PasswordPolicy(nameof(Username), nameof(Password))]
[AsyncRegistrationScreen]
public class UserRegistration
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [UniqueUsername]
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    [UniqueEmail]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }
}
