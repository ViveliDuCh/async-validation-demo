// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MixedSyncAsyncConsole;

/// <summary>
/// Nested POCO validated via [ValidateObjectMembers] on the parent.
/// Demonstrates that the async pipeline validates nested objects in parallel
/// with top-level properties.
/// </summary>
public class SmtpCredentials
{
    [Required(ErrorMessage = "SMTP username is required.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "SMTP password is required.")]
    [MinLength(8, ErrorMessage = "SMTP password must be at least 8 characters.")]
    public string Password { get; set; } = "";
}
