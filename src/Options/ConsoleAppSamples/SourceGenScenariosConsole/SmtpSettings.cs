// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace SourceGenScenariosConsole;

public class SmtpSettings
{
    [Required(ErrorMessage = "SMTP host is required.")]
    [AsyncSmtpReachable]
    public string Host { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 587;

    public bool UseTls { get; set; } = true;

    [ValidateObjectMembers]
    public SmtpCredentials Credentials { get; set; } = new();
}
