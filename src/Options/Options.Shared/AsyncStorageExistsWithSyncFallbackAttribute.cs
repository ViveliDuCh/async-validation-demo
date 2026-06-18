using System.ComponentModel.DataAnnotations;

namespace Options.Shared;

/// <summary>
/// Async validation attribute that ALSO provides a sync fallback.
///
/// Unlike <see cref="AsyncStorageExistsAttribute"/> (which throws on the sync path
/// to force async-only callers), this attribute overrides BOTH
/// <see cref="ValidationAttribute.IsValid(object, ValidationContext)"/> AND
/// <see cref="AsyncValidationAttribute.IsValidAsync(object, ValidationContext, CancellationToken)"/>.
///
/// Why this matters:
///   - <c>.ValidateDataAnnotations() + .ValidateOnStart()</c> wires the sync
///     <c>IValidateOptions&lt;T&gt;</c> into the host's startup pipeline.
///   - On <c>Host.StartAsync()</c>, the host calls the sync validator, which
///     calls <c>IsValid(value, ctx)</c>. If we throw here, the app crashes
///     before any user code runs.
///   - By providing a cheap sync fallback (basic well-formedness check), the
///     startup gate succeeds and the FULL async check still runs through
///     <c>IAsyncValidateOptions&lt;T&gt;</c> when explicitly invoked.
///
/// Matches the dual-mode pattern shown in
/// <c>AsyncDateRangeValidWithSyncFallbackAttribute</c>
/// (see dotnet/runtime#128096).
/// </summary>
public class AsyncStorageExistsWithSyncFallbackAttribute : AsyncValidationAttribute
{
    public AsyncStorageExistsWithSyncFallbackAttribute()
        : base("Storage endpoint could not be reached.") { }

    /// <summary>
    /// Sync fallback — runs during the sync DataAnnotations pipeline
    /// (e.g. <c>Validator.TryValidateObject</c>, <c>IValidateOptions&lt;T&gt;.Validate</c>,
    /// and the host's <c>StartupValidator</c> on <c>Host.StartAsync</c>).
    ///
    /// We do ONLY cheap, allocation-free, non-I/O checks here so the sync
    /// path can never block or throw. The real reachability probe happens
    /// in <see cref="IsValidAsync"/>.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var log = (ValidationLogService?)validationContext.GetService(typeof(ValidationLogService));

        if (value is not string endpoint || string.IsNullOrWhiteSpace(endpoint))
        {
            log?.Log("AsyncStorageExistsWithSyncFallback",
                "✗ Sync fallback FAILED — endpoint is null/empty");
            return new ValidationResult(
                "A valid endpoint string is required.",
                [validationContext.MemberName!]);
        }

        // Cheap, sync-safe well-formedness check (no I/O, no DNS, no HTTP).
        // The full reachability probe is deferred to IsValidAsync.
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            log?.Log("AsyncStorageExistsWithSyncFallback",
                $"✗ Sync fallback FAILED — '{endpoint}' is not a well-formed absolute URI");
            return new ValidationResult(
                $"Storage endpoint '{endpoint}' is not a well-formed absolute URI.",
                [validationContext.MemberName!]);
        }

        log?.Log("AsyncStorageExistsWithSyncFallback",
            $"✓ Sync fallback PASSED (well-formed) — '{endpoint}'");
        return ValidationResult.Success;
    }

    /// <summary>
    /// Async path — the real reachability probe.
    /// Called by the async DataAnnotations pipeline (<c>TryValidateObjectAsync</c>,
    /// <c>IAsyncValidateOptions&lt;T&gt;.ValidateAsync</c>, and
    /// <c>IAsyncStartupValidator</c>).
    /// </summary>
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        var log = (ValidationLogService?)validationContext.GetService(typeof(ValidationLogService));

        // Reuse the sync fallback for the cheap checks first.
        ValidationResult? syncResult = IsValid(value, validationContext);
        if (syncResult is not null && syncResult != ValidationResult.Success)
        {
            return syncResult;
        }

        string endpoint = (string)value!;

        log?.Log("AsyncStorageExistsWithSyncFallback",
            $"▶ IsValidAsync started for Endpoint = \"{endpoint}\"");

        // Simulated async probe (e.g. DNS lookup, HTTP HEAD).
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(100, cancellationToken);
        sw.Stop();

        if (endpoint.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            log?.Log("AsyncStorageExistsWithSyncFallback",
                $"✗ Async probe FAILED after {sw.ElapsedMilliseconds}ms — endpoint not reachable");
            return new ValidationResult(
                $"Storage endpoint '{endpoint}' is not reachable.",
                [validationContext.MemberName!]);
        }

        log?.Log("AsyncStorageExistsWithSyncFallback",
            $"✓ Async probe PASSED after {sw.ElapsedMilliseconds}ms — endpoint OK");
        return ValidationResult.Success;
    }
}
