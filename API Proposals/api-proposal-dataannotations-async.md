# [API Proposal]: Async Validation Support for System.ComponentModel.DataAnnotations

## Background and motivation

`System.ComponentModel.DataAnnotations` validation has been synchronous since its introduction in .NET Framework 3.5 SP1 (2008). The `Validator` class, `ValidationAttribute.IsValid`, `IValidatableObject`, and `ValidationContext` (all added in .NET Framework 4.0) form a fully synchronous pipeline. [Across the .NET product suite](https://github.com/jeffhandley/dataannotations-validation/blob/main/chapters/11-integration-history.md), DataAnnotations has been integrated into 11 distinct application models: MVC, Blazor, Options, EF Core conventions, OpenAPI schema, Minimal APIs via `Microsoft.Extensions.Validation`, CommunityToolkit.Mvvm `ObservableValidator`, the Options validation source generator, .NET Aspire, and the foundational `Validator` class itself.Every one is synchronous at the DataAnnotations level.

Modern applications frequently need to validate against external resources (database uniqueness checks, API calls, DNS lookups, license verification) and today's only option is blocking I/O inside `IsValid`.

**Concrete scenarios:**
1. A Minimal API endpoint validating a registration form: checking username uniqueness requires a database round-trip that blocks a thread pool thread.
2. A Blazor Server form where blocking I/O inside validation freezes the UI because `EditContext.Validate()` is synchronous. Blazor's component model is inherently async, and [async validation was explicitly planned in 2019](https://github.com/dotnet/aspnetcore/pull/7614) but never implemented.
3. An Options startup validator (`ValidateOnStart`) that checks a connection string is reachable. Blocking at startup delays app readiness.

**Architecture note:** 
- ASP.NET Core MVC does **not** use `Validator.TryValidateObject()`. It has its own pipeline via `DataAnnotationsModelValidator` → `ValidationAttribute.GetValidationResult()`. Changes to `Validator` alone do not automatically benefit MVC. 
- Meanwhile, `Microsoft.Extensions.Validation` (.NET 10) is async at the orchestration level but calls `IsValid()` synchronously at the leaf which makes it the closest to async-ready.

**Prior art:** The `oroztocil/validation-demo` branch in `dotnet/aspnetcore` prototyped `AsyncValidationAttribute` and `IAsyncValidatableObject` in `Microsoft.Extensions.Validation` to prove the pipeline could handle async. This proposal moves the canonical types into the core `System.ComponentModel.Annotations` library so all downstream consumers converge on a single async validation model.

**References:**
- [DataAnnotations Validation — Maintainer's Guide](https://github.com/jeffhandley/dataannotations-validation) (Jeff Handley)
- [Halter's consolidated design gist](https://gist.github.com/halter73/f4d0974da579fb78d17bd2e6d9f78173)
- Related: [dotnet/aspnetcore#46349](https://github.com/dotnet/aspnetcore/issues/46349), [dotnet/aspnetcore#60724](https://github.com/dotnet/aspnetcore/pull/60724)

## API Proposal

> **Note:** This API surface matches the [feasibility prototype](https://github.com/ViveliDuCh/runtime/tree/async-validation) (`C:\REPOS\runtime` branch `async-validation`). The design gist proposes additional surface (e.g. `IsValidAsync`/`GetValidationResultAsync` on the base `ValidationAttribute` class) that is still under active discussion. See [Open Questions](#open-questions) below.

```csharp
namespace System.ComponentModel.DataAnnotations;

// New abstract class deriving from ValidationAttribute
public abstract partial class AsyncValidationAttribute : ValidationAttribute
{
    protected AsyncValidationAttribute();
    protected AsyncValidationAttribute(Func<string> errorMessageAccessor);
    protected AsyncValidationAttribute(string errorMessage);

    // Sync IsValid throws NotSupportedException — forces callers to use the async path.
    // Virtual (not sealed): subclasses may override to provide a sync fallback.
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext);

    // Async override point for subclasses
    protected abstract ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken);

    // Public async entry point — counterpart to GetValidationResult.
    // Calls IsValidAsync, populates error message via FormatErrorMessage on null/empty.
    public ValueTask<ValidationResult?> GetValidationResultAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);
}

// New interface for object-level async validation (standalone — does NOT extend IValidatableObject)
public partial interface IAsyncValidatableObject
{
    ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);
}

// Async counterparts on the existing Validator static class
public static partial class Validator
{
    // EXISTING (shown for context)
    // public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult>? validationResults, bool validateAllProperties);
    // public static bool TryValidateProperty(object? value, ValidationContext validationContext, ICollection<ValidationResult>? validationResults);
    // public static bool TryValidateValue(object? value, ValidationContext validationContext, ICollection<ValidationResult>? validationResults, IEnumerable<ValidationAttribute> validationAttributes);
    // public static void ValidateObject(object instance, ValidationContext validationContext, bool validateAllProperties);
    // public static void ValidateProperty(object? value, ValidationContext validationContext);
    // public static void ValidateValue(object? value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes);

    public static ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ICollection<ValidationResult>? validationResults,
        CancellationToken cancellationToken = default);

    public static ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ICollection<ValidationResult>? validationResults,
        bool validateAllProperties,
        CancellationToken cancellationToken = default);

    public static ValueTask<bool> TryValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        ICollection<ValidationResult>? validationResults,
        CancellationToken cancellationToken = default);

    public static ValueTask<bool> TryValidateValueAsync(
        object? value,
        ValidationContext validationContext,
        ICollection<ValidationResult>? validationResults,
        IEnumerable<ValidationAttribute> validationAttributes,
        CancellationToken cancellationToken = default);

    public static ValueTask ValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);

    public static ValueTask ValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        bool validateAllProperties,
        CancellationToken cancellationToken = default);

    public static ValueTask ValidatePropertyAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);

    public static ValueTask ValidateValueAsync(
        object? value,
        ValidationContext validationContext,
        IEnumerable<ValidationAttribute> validationAttributes,
        CancellationToken cancellationToken = default);
}
```

Prototype: https://github.com/ViveliDuCh/runtime/tree/async-validation

## API Usage

### Scenario 1: No interface — mixed async and sync property- and entity-level attributes

A plain class (no `IValidatableObject` / `IAsyncValidatableObject`) decorated with both
sync (`ValidationAttribute`) and async (`AsyncValidationAttribute`) attributes at the
property and class level. `TryValidateObjectAsync` runs sync attrs first, then async.

```csharp
// Sync property attribute (standard)
public class IsValidNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        Thread.Sleep(50); // Simulates sync I/O (blocks thread)
        return ValidationResult.Success;
    }
}

// Async property attribute (async-only, no sync fallback)
public class AsyncEmailDomainAttribute : AsyncValidationAttribute
{
    private readonly string _requiredDomain;

    public AsyncEmailDomainAttribute(string requiredDomain)
        : base($"Email must belong to the '{requiredDomain}' domain.")
        => _requiredDomain = requiredDomain;

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string email)
            return new ValidationResult("A valid email string is required.");

        await Task.Delay(50, cancellationToken); // Non-blocking I/O

        return email.EndsWith($"@{_requiredDomain}", StringComparison.OrdinalIgnoreCase)
            ? ValidationResult.Success
            : new ValidationResult($"'{email}' is not in the '{_requiredDomain}' domain.",
                  new[] { validationContext.MemberName! });
    }
}

// Async entity-level attribute (applied to the class)
[AttributeUsage(AttributeTargets.Class)]
public class AsyncDateRangeValidAttribute : AsyncValidationAttribute
{
    private readonly string _startProp;
    private readonly string _endProp;

    public AsyncDateRangeValidAttribute(string startProp, string endProp)
    { _startProp = startProp; _endProp = endProp; }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulates calendar service call

        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var start = (DateTime?)type.GetProperty(_startProp)?.GetValue(instance);
        var end = (DateTime?)type.GetProperty(_endProp)?.GetValue(instance);

        return start.HasValue && end.HasValue && start.Value >= end.Value
            ? new ValidationResult($"'{_startProp}' must be before '{_endProp}'.",
                  new[] { _startProp, _endProp })
            : ValidationResult.Success;
    }
}

// Model: sync + async property attrs, async class-level attr, NO interface
[AsyncDateRangeValid(nameof(StartDate), nameof(EndDate))]
public class Event
{
    [Required]                       // sync property attr
    public string? Title { get; set; }

    [Required]                       // sync property attr
    public DateTime? StartDate { get; set; }

    [Required]                       // sync property attr
    public DateTime? EndDate { get; set; }
}

public class User
{
    [Required]                       // sync property attr
    [IsValidName]                    // sync property attr (Thread.Sleep)
    public string? Name { get; set; }

    [Required]                       // sync property attr
    [AsyncEmailDomain("contoso.com")]// async property attr (Task.Delay)
    public string? Email { get; set; }
}

// Validation: three-phase — sync attrs first, async attrs in parallel, then object-level
var user = new User { Name = "Bob", Email = "bob@gmail.com" };
var results = new List<ValidationResult>();
bool valid = await Validator.TryValidateObjectAsync(
    user, new ValidationContext(user), results, validateAllProperties: true);
// Phase 1: All properties validated in parallel. Per property: sync attrs first
// Phase 2: Per property: [AsyncEmailDomain] runs asynchronously (parallel across properties)
// Phase 3: IAsyncValidatableObject / IValidatableObject (if any)
// valid == false, results: "'bob@gmail.com' is not in the 'contoso.com' domain."

// Two-phase optimization: sync failure skips async entirely
var badUser = new User { Name = "", Email = "bob@gmail.com" }; // [Required] fails
results.Clear();
valid = await Validator.TryValidateObjectAsync(
    badUser, new ValidationContext(badUser), results, true);
// [Required] fails on Name → [AsyncEmailDomain] never runs → no I/O wasted
```

### Scenario 2: IValidatableObject with mixed async and sync attributes

A class that implements the existing sync `IValidatableObject` interface alongside
both sync and async property-level attributes. `TryValidateObjectAsync` runs
property-level attrs (sync then async), then calls `IValidatableObject.Validate()`.

```csharp
public class Order : IValidatableObject
{
    [Required]                       // sync property attr
    public string? ProductName { get; set; }

    [Required]                       // sync property attr
    [Range(1, 10_000)]               // sync property attr
    public int Quantity { get; set; }

    [Required]                       // sync property attr
    [Range(0.01, double.MaxValue)]   // sync property attr
    public decimal UnitPrice { get; set; }

    // IValidatableObject.Validate — sync cross-property logic
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        Thread.Sleep(50); // Simulates sync inventory check (blocks thread)

        decimal totalCost = Quantity * UnitPrice;
        if (totalCost > 50_000m)
        {
            yield return new ValidationResult(
                $"Total cost ({totalCost:C}) exceeds the $50,000 limit.",
                new[] { nameof(Quantity), nameof(UnitPrice) });
        }
    }
}

// TryValidateObjectAsync works with IValidatableObject — calls Validate() synchronously
// after property-level validation passes. Property validation runs in parallel across properties.
var order = new Order { ProductName = "Widget", Quantity = 10_000, UnitPrice = 10m };
var results = new List<ValidationResult>();
bool valid = await Validator.TryValidateObjectAsync(
    order, new ValidationContext(order), results, true);
// Phase 1: sync property attrs validated in parallel across all properties → pass
// Phase 2: no async property attrs → skipped
// IValidatableObject.Validate() runs → total $100k > $50k → fails
```

### Scenario 3: IAsyncValidatableObject with mixed async and sync attributes

A class that implements the new `IAsyncValidatableObject` interface for async
cross-property validation, decorated with both sync and async property-level attributes.

```csharp
public class MoneyTransfer : IAsyncValidatableObject
{
    [Required]                       // sync property attr
    public string? FromAccount { get; set; }

    [Required]                       // sync property attr
    public string? ToAccount { get; set; }

    [Range(0.01, double.MaxValue)]   // sync property attr
    public decimal Amount { get; set; }

    // IAsyncValidatableObject.ValidateAsync — async cross-property logic
    public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
        ValidationContext validationContext, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationResult>();

        // Sync cross-property check (no I/O needed)
        if (FromAccount == ToAccount)
        {
            errors.Add(new ValidationResult(
                "Cannot transfer to the same account.",
                new[] { nameof(FromAccount), nameof(ToAccount) }));
        }

        // Async balance check — frees the thread
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

// TryValidateObjectAsync prefers IAsyncValidatableObject over IValidatableObject
var transfer = new MoneyTransfer
{
    FromAccount = "checking", ToAccount = "checking", Amount = 1000.00m
};
var results = new List<ValidationResult>();
bool valid = await Validator.TryValidateObjectAsync(
    transfer, new ValidationContext(transfer), results, true);
// Phase 1: sync [Required]/[Range] validated in parallel across properties → pass
// Phase 2: no async property attrs → skipped
// IAsyncValidatableObject.ValidateAsync() runs:
//   → same account error + insufficient funds error
```

### Scenario 4: Async attribute with sync fallback (sync-over-async via Task.Result)

An `AsyncValidationAttribute` that also overrides the sync `IsValid` for backward
compatibility with sync callers (e.g., `Validator.TryValidateObject`). The sync path
uses `.GetAwaiter().GetResult()` — blocking but functional.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class AsyncDateRangeValidWithSyncFallback : AsyncValidationAttribute
{
    private readonly string _startProp;
    private readonly string _endProp;

    public AsyncDateRangeValidWithSyncFallback(string startProp, string endProp)
    { _startProp = startProp; _endProp = endProp; }

    // Async path — used by TryValidateObjectAsync (non-blocking)
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulates async calendar check

        return ValidateDateRange(validationContext);
    }

    // Sync fallback — used by TryValidateObject (blocks the thread)
    // Overrides the base AsyncValidationAttribute.IsValid which throws NotSupportedException
    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        // Sync-over-async: blocks the calling thread via .Result
        // This is intentional for backward compat with sync-only callers
        Thread.Sleep(50); // Simulates sync calendar check

        return ValidateDateRange(validationContext);
    }

    // Shared validation logic (no I/O)
    private ValidationResult? ValidateDateRange(ValidationContext validationContext)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var start = (DateTime?)type.GetProperty(_startProp)?.GetValue(instance);
        var end = (DateTime?)type.GetProperty(_endProp)?.GetValue(instance);

        return start.HasValue && end.HasValue && start.Value >= end.Value
            ? new ValidationResult($"'{_startProp}' must be before '{_endProp}'.",
                  new[] { _startProp, _endProp })
            : ValidationResult.Success;
    }
}

// Usage on a model
[AsyncDateRangeValidWithSyncFallback(nameof(StartDate), nameof(EndDate))]
public class Event
{
    [Required]
    public string? Title { get; set; }
    [Required]
    public DateTime? StartDate { get; set; }
    [Required]
    public DateTime? EndDate { get; set; }
}

var badEvent = new Event
{
    Title = "Party", StartDate = new DateTime(2026, 12, 25), EndDate = new DateTime(2026, 12, 20)
};

// Async path: non-blocking
var results = new List<ValidationResult>();
bool valid = await Validator.TryValidateObjectAsync(
    badEvent, new ValidationContext(badEvent), results, true);
// Calls IsValidAsync → await Task.Delay → returns error

// Sync path: works too (blocks thread, but doesn't throw NotSupportedException)
results.Clear();
valid = Validator.TryValidateObject(
    badEvent, new ValidationContext(badEvent), results, true);
// Calls IsValid (sync override) → Thread.Sleep → returns same error

// CONTRAST: an async-only attribute (no sync override) throws on the sync path:
// Validator.TryValidateObject(userWithAsyncOnlyAttr, ...) → NotSupportedException
```

## Alternative Designs

**Option A: Virtual `IsValidAsync` on `ValidationAttribute` directly (no subclass)**
    
  - Reflection-based override detection is fragile, and a virtual `IsValidAsync` that throws by default confuses the existing 200+ `ValidationAttribute` subclasses.

**Option B: Separate `AsyncValidationAttribute` NOT deriving from `ValidationAttribute`**
   
  - Sync `Validator.TryValidateObject` uses `GetCustomAttributes<ValidationAttribute>()` and would silently skip it (not desired).

**Option C (chosen): `AsyncValidationAttribute` deriving from `ValidationAttribute`**
  
  - The sync `IsValid` override throws `NotSupportedException`, forcing async callers. Since `AsyncValidationAttribute` IS-A `ValidationAttribute`, sync `Validator` still discovers it via reflection and produces a clear error.

**Option D: `IAsyncValidationAttribute` interface**
  
  - Less discoverable. Users must know to implement an interface AND inherit `ValidationAttribute`. The subclass approach is more idiomatic for DataAnnotations.

**Key behaviors in the prototype:**

| Attribute type | Sync path (`GetValidationResult`) | Async path (`GetValidationResultAsync`) |
|---|---|---|
| Traditional `ValidationAttribute` subclass | ✅ Works normally | ✅ Async `Validator` delegates to sync `IsValid` internally |
| `AsyncValidationAttribute` (async-only) | ❌ Throws `NotSupportedException` | ✅ Calls `IsValidAsync` |
| `AsyncValidationAttribute` with sync override | ✅ Uses `IsValid` override | ✅ Calls `IsValidAsync` |

**Parallel execution model:**

The async `Validator` methods (`TryValidateObjectAsync`, `TryValidateValueAsync`) execute validation in parallel at two levels to maximize throughput for I/O-bound validators:

1. **Per-property parallelism** (`GetObjectPropertyValidationErrorsAsync`): When `validateAllProperties` is `true`, all properties are validated concurrently via `Task.WhenAll`. Each property's validation is started immediately, and errors are collected after all complete. This means N properties with async attributes complete in ~max(property_time) instead of ~sum(property_time).

2. **Per-attribute parallelism** (`GetValidationErrorsAsync` Phase 2): Within a single property, all `AsyncValidationAttribute` instances are started concurrently via `Task.WhenAll`. Sync attributes still run first (Phase 1), and async attributes only run if all sync attributes pass. But when Phase 2 runs, all async attributes execute in parallel.

The two-phase guarantee is preserved:
- **Phase 1 (sync):** `RequiredAttribute` runs first. If it fails, validation stops immediately. Other sync `ValidationAttribute` subclasses run next.
- **Phase 2 (async, parallel):** Only if Phase 1 produces zero errors. All `AsyncValidationAttribute` instances are started concurrently, results collected after all complete.
- **Phase 3 (object-level):** `IAsyncValidatableObject.ValidateAsync()` or `IValidatableObject.Validate()` runs only if Phases 1+2 produce zero errors.

| Aspect | Sync `Validator` | Async `Validator` |
|--------|-----------------|-------------------|
| Property execution order | Sequential | **Parallel** (all properties concurrently) |
| Sync attribute execution | Sequential per property | Sequential per property |
| Async attribute execution | N/A | **Parallel** (all async attrs on a property concurrently) |
| `breakOnFirstError` | Stops at first error | Per-property: stops sync phase. Cross-property: all run (can't short-circuit parallel) |
| Thread safety requirement | Single-threaded | Validators must be safe for concurrent execution |

## Notes/Risks

- **Source breaking:** The new `Validator.*Async` methods follow the established `XAsync` naming pattern with distinct signatures (return `ValueTask`). No ambiguity with existing sync methods. Risk: **none**.
- **Binary breaking:** All additions are additive. No existing APIs changed. Risk: **none**.
- **Behavioral:** Sync `Validator.TryValidateObject` discovering an `AsyncValidationAttribute` will throw `NotSupportedException` instead of silently succeeding. This is **by design**: it surfaces the mismatch between sync callers and async-only attributes.
- **Parallel execution:** The async `Validator` validates all properties concurrently and all async attributes on each property concurrently. This is a deliberate design choice for I/O-bound validators. Validators must not rely on execution order across properties. The `breakOnFirstError` parameter still applies within a property's sync phase, but cannot short-circuit across properties once parallel execution has started.

## Open Questions

1. Should `IsValidAsync` / `GetValidationResultAsync` be added to the base `ValidationAttribute`?
2. `IAsyncValidatableObject` return type: `ValueTask<IEnumerable<>>` vs `IAsyncEnumerable<>`
3. `ValueTask<T>` vs `Task<T>` for async validation methods
4. **Sync-over-async escape hatch**: Whether to provide an explicit opt-in sync-over-async mechanism. Some suggest delaying until there are clear customer requests; others note users can already block on async APIs manually.
5. Progressive validation / partial results: should the current API shape be **forward-compatible** with progressive validation (e.g., `IProgress<ValidationResult>` callback, or `IAsyncEnumerable` return type on Q2)?


---

## Next Steps

### UX-Related API Gaps

- **Pre-validation rule descriptions**: Validation attributes should be able to define/return a message describing the validation rule *before* execution, so UI can show rules upfront.
- **"Validation in progress" messaging**: Async validation attributes should provide a message while validation is running, so UX can indicate pending state.
- **Detecting presence of async validators**: Possible need for an API to quickly determine whether any async validators are involved, enabling frameworks to choose between sync and async UX paths. *Noted as lower priority and possibly deferrable.*

### Localization Considerations

- With parallel execution now implemented, **localization must be considered**.
- Reuse existing `ErrorMessage` localization patterns (string vs resource-based) for consistency.
- **Attribute verbosity risk**: Adding more message-related APIs could make validation attribute declarations overly verbose.
- **Thread safety**: Since async validators run concurrently, error message formatting and resource access must be thread-safe. The existing `FormatErrorMessage` pattern is safe (stateless string formatting), but custom validators that access shared mutable state during error message construction must synchronize.

