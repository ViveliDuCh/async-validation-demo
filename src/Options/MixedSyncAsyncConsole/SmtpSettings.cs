// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace MixedSyncAsyncConsole;

/// <summary>
/// Options POCO demonstrating mixed sync + async validation on the same type.
/// 
/// Sync attributes:  [Required], [Range]       → run in both sync and async paths
/// Dual-mode attr:   [AsyncSmtpReachable]       → sync fallback in ValidateDataAnnotations(),
///                                                async path in ValidateDataAnnotationsAsync()
/// Nested object:    [ValidateObjectMembers]    → async pipeline validates nested SmtpCredentials in parallel
/// </summary>
public class SmtpSettings
{
    /// <summary>SMTP host — validated asynchronously for reachability.</summary>
    [Required(ErrorMessage = "SMTP host is required.")]
    [AsyncSmtpReachable]
    public string Host { get; set; } = "";

    /// <summary>SMTP port — sync range validation.</summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 587;

    /// <summary>Whether to use TLS.</summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// Nested credentials — validated via [ValidateObjectMembers],
    /// which causes the async pipeline to validate this sub-object
    /// in parallel with the top-level properties.
    /// </summary>
    [ValidateObjectMembers]
    public SmtpCredentials Credentials { get; set; } = new();
}
