// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace StartupValidatorConsole;

public class DatabaseSettings
{
    [Required(ErrorMessage = "ConnectionString is required.")]
    public string ConnectionString { get; set; } = "";
}

public class AppSettings
{
    [Required(ErrorMessage = "AppName is required.")]
    public string AppName { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; }
}

public class SimpleSettings
{
    public string Name { get; set; } = "";
}

public class SmtpSettings
{
    [Required(ErrorMessage = "SmtpHost is required.")]
    public string SmtpHost { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "SmtpPort must be between 1 and 65535.")]
    public int SmtpPort { get; set; }
}
