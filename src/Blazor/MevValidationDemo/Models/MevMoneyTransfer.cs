using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Validation;

namespace MevValidationDemo.Models;

[Microsoft.Extensions.Validation.ValidatableType]
public partial class MevMoneyTransfer : IAsyncValidatableObject
{
    [Required]
    public string? FromAccount { get; set; }

    [Required]
    public string? ToAccount { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationResult>();

        if (FromAccount == ToAccount)
        {
            errors.Add(new ValidationResult(
                "Cannot transfer to the same account.",
                new[] { nameof(FromAccount), nameof(ToAccount) }));
        }

        await Task.Delay(50, cancellationToken);
        decimal balance = 500.00m;

        if (Amount > balance)
        {
            errors.Add(new ValidationResult(
                $"Insufficient funds. Balance: ${balance:F2}, Transfer: ${Amount:F2}.",
                new[] { nameof(Amount) }));
        }

        return errors;
    }
}
