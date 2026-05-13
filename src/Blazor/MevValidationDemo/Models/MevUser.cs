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
