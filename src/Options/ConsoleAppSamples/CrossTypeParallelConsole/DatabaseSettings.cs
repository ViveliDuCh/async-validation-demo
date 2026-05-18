// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace CrossTypeParallelConsole;

/// <summary>
/// Database connection options — validated asynchronously at startup.
/// The [AsyncConnectionReachable] attribute simulates a 200ms connectivity check.
/// </summary>
public class DatabaseSettings
{
    [Required(ErrorMessage = "Connection string is required.")]
    [AsyncConnectionReachable]
    public string ConnectionString { get; set; } = "";

    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds.")]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
