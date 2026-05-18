// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace SourceGenScenariosConsole;

public class SmtpCredentials
{
    [Required(ErrorMessage = "SMTP username is required.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "SMTP password is required.")]
    [MinLength(8, ErrorMessage = "SMTP password must be at least 8 characters.")]
    public string Password { get; set; } = "";
}
