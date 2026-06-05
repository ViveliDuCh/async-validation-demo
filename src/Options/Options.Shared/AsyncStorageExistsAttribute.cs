using System.ComponentModel.DataAnnotations;

namespace Options.Shared;

/// <summary>
/// Async validation attribute that simulates verifying
/// a storage endpoint is reachable (e.g., DNS lookup, HTTP probe).
/// Follows the same pattern as AsyncOnlyEmailDomainAttribute in SharedModels.
/// </summary>
public class AsyncStorageExistsAttribute : AsyncValidationAttribute
{
    public AsyncStorageExistsAttribute()
        : base("Storage endpoint could not be reached.") { }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        // Sync fallback: throw NotSupportedException to enforce async path
        throw new NotSupportedException(
            $"{nameof(AsyncStorageExistsAttribute)} requires async validation. " +
            "Use the async pipeline (ValidateDataAnnotationsAsync / TryValidateObjectAsync).");
    }

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
            return new ValidationResult("A valid endpoint string is required.");

        // Simulate an async network probe (e.g., DNS check, HTTP HEAD)
        await Task.Delay(100, cancellationToken);

        // Fail if endpoint contains "invalid" — simple test hook
        if (endpoint.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Storage endpoint '{endpoint}' is not reachable.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
