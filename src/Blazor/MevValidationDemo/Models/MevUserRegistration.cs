using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;
using SharedModels.ValidationClasses;

namespace MevValidationDemo.Models;

/// <summary>
/// Local model mirroring SharedModels.UserRegistration but with [ValidatableType]
/// to activate the MEV source-gen code path in DataAnnotationsValidator.
/// Reuses the same async validation attributes from SharedModels.
/// </summary>
[Microsoft.Extensions.Validation.ValidatableType]
[PasswordPolicy(nameof(Username), nameof(Password))]
[AsyncRegistrationScreen]
public partial class MevUserRegistration
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
