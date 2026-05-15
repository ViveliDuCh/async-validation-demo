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

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        var log = validationContext.GetService(typeof(ValidationLogService))
            as ValidationLogService;

        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
            return new ValidationResult("A valid endpoint string is required.");

        log?.Log("AsyncStorageExistsAttribute",
            $"▶ IsValidAsync started for Endpoint = \"{endpoint}\"");

        // Simulate an async network probe (e.g., DNS check, HTTP HEAD)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(100, cancellationToken);
        sw.Stop();

        // Fail if endpoint contains "invalid" — simple test hook
        if (endpoint.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            log?.Log("AsyncStorageExistsAttribute",
                $"✗ Validation FAILED after {sw.ElapsedMilliseconds}ms (async!) — endpoint not reachable");
            return new ValidationResult(
                $"Storage endpoint '{endpoint}' is not reachable.",
                new[] { validationContext.MemberName! });
        }

        log?.Log("AsyncStorageExistsAttribute",
            $"✓ Validation PASSED after {sw.ElapsedMilliseconds}ms (async!) — endpoint OK");
        return ValidationResult.Success;
    }

    // IsValid is intentionally NOT overridden.
    // Base AsyncValidationAttribute.IsValid throws NotSupportedException,
    // enforcing the async path — same pattern as AsyncOnlyEmailDomainAttribute.
}
