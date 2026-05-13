// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.ValidationClasses;

namespace SharedModels.EntityClasses;

/// <summary>
/// API Proposal Scenario 1: No interface — mixed sync + async property-level attributes.
/// Matches the User entity from dotnet/runtime#128096.
/// </summary>
public class User
{
    [Required]
    [IsValidName]
    public string? Name { get; set; }

    [Required]
    [UsernameAvailableAsync]
    public string? Username { get; set; }
}
