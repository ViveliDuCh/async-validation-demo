// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;
using SharedModels.ValidationClasses;

namespace MevValidationDemo.Models;

[Microsoft.Extensions.Validation.ValidatableType]
public partial class MevUser
{
    [Required]
    [IsValidName]
    public string? Name { get; set; }

    [Required]
    [UsernameAvailableAsync]
    public string? Username { get; set; }
}
