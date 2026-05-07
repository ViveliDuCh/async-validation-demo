// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Copy of UserRegistration using the ISchemaDescriptor-enabled attributes.
/// Shows what an entity looks like when its async attributes opt into schema visibility.
/// </summary>
public class UserRegistrationWithDescriptor
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [UniqueUsernameDescriptor]
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    [UniqueEmailDescriptor]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }
}
