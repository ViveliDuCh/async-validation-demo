using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyAttributeConventionDemo;

/// <summary>
/// Entity mixing sync DataAnnotations (built-in) with an async validation
/// attribute (custom). Self-contained — no SharedModels dependency.
/// </summary>
[Table("Users")]
public class UserRegistration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [UniqueUsername]
    public string Username { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";
}
