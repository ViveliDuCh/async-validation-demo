> ## ⚠️ Merged API delta (post-PR #128656)
>
> This proposal reflects the **prototype**; the API as merged into `dotnet/runtime` `main` differs in the following ways. Samples in [this repo](https://github.com/ViveliDuCh/async-validation-demo/tree/api-proposal-samples) have been updated to match the merged surface.
>
> | Surface | Proposal | **Merged** |
> |---|---|---|
> | `AsyncValidationAttribute.IsValidAsync` | `ValueTask<ValidationResult?>` | **`Task<ValidationResult?>`** |
> | `AsyncValidationAttribute.GetValidationResultAsync` | `ValueTask<…>` | **`Task<…>`** |
> | `Validator.TryValidate*Async` / `Validate*Async` | `ValueTask<bool>` / `ValueTask` | **`Task<bool>` / `Task`** |
> | `AsyncValidationAttribute.IsValid(value, ctx)` | `protected override`, throws by default | **`protected abstract override`** — every subclass must implement a sync fallback (or throw). Scenario 4 (sync fallback) is therefore the *required* shape, not optional. |
> | `AsyncValidationAttribute.IsValid(value)` | inherited | **`public sealed override`** that delegates to `IsValid(value, null!)` |
> | `IAsyncValidatableObject` | DIM throws `InvalidOperationException` on `Validate` | **No DIM.** Implementers must provide `IValidatableObject.Validate` themselves (throw `InvalidOperationException` or do real work). |
>
> See merged sources:
> - [`AsyncValidationAttribute.cs`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/AsyncValidationAttribute.cs)
> - [`IAsyncValidatableObject.cs`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/IAsyncValidatableObject.cs)
> - [Ref assembly](https://github.com/dotnet/runtime/blob/main/src/libraries/System.ComponentModel.Annotations/ref/System.ComponentModel.Annotations.cs)
>
> Everything below describes the original proposal. Treat `ValueTask` → `Task` and "throws by default" → "abstract" when reading.

### Background and motivation

`System.ComponentModel.DataAnnotations` validation has been synchronous since its introduction in .NET Framework 3.5 SP1 (2008). The `Validator` class, `ValidationAttribute.IsValid`, `IValidatableObject`, and `ValidationContext` (all added in .NET Framework 4.0) form a fully synchronous pipeline. [Across the .NET product suite](https://github.com/jeffhandley/dataannotations-validation/blob/main/chapters/11-integration-history.md), DataAnnotations has been integrated into 11 distinct application models: MVC, Blazor, Options, EF Core conventions, OpenAPI schema, Minimal APIs via `Microsoft.Extensions.Validation`, CommunityToolkit.Mvvm `ObservableValidator`, the Options validation source generator, .NET Aspire, and the foundational `Validator` class itself. Every one is synchronous at the DataAnnotations level.

Modern applications frequently need to validate against external resources (database uniqueness checks, async API calls) and today's only option is blocking I/O inside `IsValid`.

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
- Related: [dotnet/runtime#121536](https://github.com/dotnet/runtime/issues/121536) [dotnet/aspnetcore#46349](https://github.com/dotnet/aspnetcore/issues/46349), [dotnet/aspnetcore#60724](https://github.com/dotnet/aspnetcore/pull/60724)

### API Proposal

> **Note:** This API surface matches the [feasibility prototype](https://github.com/ViveliDuCh/runtime/tree/async-validation).

```csharp
namespace System.ComponentModel.DataAnnotations;

// New abstract class deriving from ValidationAttribute
public abstract partial class AsyncValidationAttribute : ValidationAttribute
{
    protected AsyncValidationAttribute();
    protected AsyncValidationAttribute(Func<string> errorMessageAccessor);
    protected AsyncValidationAttribute(string errorMessage);

    // Sync IsValid throws InvalidOperationException, forcing callers to use the async path.
    // Virtual (not sealed): subclasses may override to provide a sync fallback.
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext);

    // Async override point for subclasses
    protected abstract ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken);

    // Public async entry point, counterpart to GetValidationResult.
    // Calls IsValidAsync, populates error message via FormatErrorMessage on null/empty.
    public ValueTask<ValidationResult?> GetValidationResultAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);
}

// New interface for object-level async validation.
// Inherits from IValidatableObject with a DIM that throws InvalidOperationException,
// mirroring the AsyncValidationAttribute pattern where sync paths fail clearly
// rather than silently skipping async validation.
public partial interface IAsyncValidatableObject : IValidatableObject
{
    IEnumerable<ValidationResult> IValidatableObject.Validate(
        ValidationContext validationContext) =>
        throw new InvalidOperationException(
            "This object implements IAsyncValidatableObject and supports only " +
            "asynchronous validation. Use the async Validator methods.");

    IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken = default);
}

// Async counterparts on the existing Validator static class
public static partial class Validator
{
    // Existing sync methods (unchanged)
    public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult>? validationResults);
    public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult>? validationResults, bool validateAllProperties);
    public static bool TryValidateProperty(object? value, ValidationContext validationContext, ICollection<ValidationResult>? validationResults);
    public static bool TryValidateValue(object? value, ValidationContext validationContext, ICollection<ValidationResult>? validationResults, IEnumerable<ValidationAttribute> validationAttributes);
    public static void ValidateObject(object instance, ValidationContext validationContext);
    public static void ValidateObject(object instance, ValidationContext validationContext, bool validateAllProperties);
    public static void ValidateProperty(object? value, ValidationContext validationContext);
    public static void ValidateValue(object? value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes);

    // New async methods
    public static ValueTask<bool> TryValidateObjectAsync(
        object instance,
        ValidationContext validationContext,
        ICollection<ValidationResult>? validationResults,
        CancellationToken cancellationToken = default);

    // validateAllProperties: when true, validates all properties; when false, only [Required] properties.
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

    // validateAllProperties: when true, validates all properties; when false, only [Required] properties.
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

**Sync/async dispatch behavior:**

| Attribute type | Sync path (`GetValidationResult`) | Async path (`GetValidationResultAsync`) |
|---|---|---|
| Traditional `ValidationAttribute` subclass | ✅ Works normally | ✅ Async `Validator` delegates to sync `IsValid` internally |
| `AsyncValidationAttribute` (async-only) | ❌ Throws `InvalidOperationException` | ✅ Calls `IsValidAsync` |
| `AsyncValidationAttribute` with sync override | ✅ Uses `IsValid` override | ✅ Calls `IsValidAsync` |

Prototype: https://github.com/ViveliDuCh/runtime/tree/async-validation



### API Usage

See full samples covering the following scenarios [here](https://github.com/ViveliDuCh/async-validation-demo/tree/api-proposal-samples).

### Scenario 1: No interface, mixed async and sync property- and entity-level attributes

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

// Async property attribute: checks username availability against a database
public class UsernameAvailableAsyncAttribute : AsyncValidationAttribute
{
    public UsernameAvailableAsyncAttribute()
        : base("The username is already taken.") { }

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (value is not string username || string.IsNullOrWhiteSpace(username))
            return ValidationResult.Success; // Let [Required] handle nulls

        // Simulates a database round-trip to check uniqueness
        await Task.Delay(200, cancellationToken);
        bool isTaken = username.Equals("admin", StringComparison.OrdinalIgnoreCase);

        return isTaken
            ? new ValidationResult($"The username '{username}' is already taken.",
                  new[] { validationContext.MemberName! })
            : ValidationResult.Success;
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
        // Simulates calling a calendar/scheduling service to get max allowed date
        await Task.Delay(50, cancellationToken);
        DateTime maxDateAllowed = DateTime.UtcNow.AddYears(1); // Service response

        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var start = (DateTime?)type.GetProperty(_startProp)?.GetValue(instance);
        var end = (DateTime?)type.GetProperty(_endProp)?.GetValue(instance);

        if (start.HasValue && end.HasValue && start.Value >= end.Value)
            return new ValidationResult($"'{_startProp}' must be before '{_endProp}'.",
                  new[] { _startProp, _endProp });

        if (end.HasValue && end.Value > maxDateAllowed)
            return new ValidationResult(
                $"'{_endProp}' cannot be later than {maxDateAllowed:d} (service limit).",
                new[] { _endProp });

        return ValidationResult.Success;
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
    [UsernameAvailableAsync]          // async property attr (DB round-trip)
    public string? Username { get; set; }
}

// Validation: three-phase (sync attrs first, async attrs in parallel, then object-level)
var user = new User { Name = "Bob", Username = "admin" };
var results = new List<ValidationResult>();
bool valid = await Validator.TryValidateObjectAsync(
    user, new ValidationContext(user), results, validateAllProperties: true);
// Phase 1: All properties validated in parallel. Per property: sync attrs first
// Phase 2: Per property: [UsernameAvailableAsync] runs asynchronously (parallel across properties)
// Phase 3: IAsyncValidatableObject / IValidatableObject (if any)
// valid == false, results: "The username 'admin' is already taken."

// Two-phase optimization: sync failure skips async entirely
var badUser = new User { Name = "", Username = "admin" }; // [Required] fails
results.Clear();
valid = await Validator.TryValidateObjectAsync(
    badUser, new ValidationContext(badUser), results, true);
// [Required] fails on Name → [UsernameAvailableAsync] never runs → no I/O wasted
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

    // IValidatableObject.Validate: sync cross-property logic
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

// TryValidateObjectAsync works with IValidatableObject, calling Validate() synchronously
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

    // IAsyncValidatableObject.ValidateAsync: async cross-property logic (streaming)
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Sync cross-property check (no I/O needed)
        if (FromAccount == ToAccount)
        {
            yield return new ValidationResult(
                "Cannot transfer to the same account.",
                new[] { nameof(FromAccount), nameof(ToAccount) });
        }

        // Async balance check (frees the thread)
        await Task.Delay(50, cancellationToken);
        decimal balance = 500.00m;

        if (Amount > balance)
        {
            yield return new ValidationResult(
                $"Insufficient funds. Balance: ${balance:F2}, Transfer: ${Amount:F2}.",
                new[] { nameof(Amount) });
        }
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
uses `.GetAwaiter().GetResult()`, blocking but functional.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class AsyncDateRangeValidWithSyncFallback : AsyncValidationAttribute
{
    private readonly string _startProp;
    private readonly string _endProp;

    public AsyncDateRangeValidWithSyncFallback(string startProp, string endProp)
    { _startProp = startProp; _endProp = endProp; }

    // Async path: used by TryValidateObjectAsync (non-blocking)
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulates async calendar check

        return ValidateDateRange(validationContext);
    }

    // Sync fallback: used by TryValidateObject (blocks the thread)
    // Overrides the base AsyncValidationAttribute.IsValid which throws InvalidOperationException
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

// Sync path: works too (blocks thread, but doesn't throw InvalidOperationException)
results.Clear();
valid = Validator.TryValidateObject(
    badEvent, new ValidationContext(badEvent), results, true);
// Calls IsValid (sync override) → Thread.Sleep → returns same error

// CONTRAST: an async-only attribute (no sync override) throws on the sync path:
// Validator.TryValidateObject(userWithAsyncOnlyAttr, ...) → InvalidOperationException
```

### Alternative Designs

**Option A: Virtual `IsValidAsync` on `ValidationAttribute` directly (no subclass)**
    
  - Reflection-based override detection is fragile, and a virtual `IsValidAsync` that throws by default confuses the existing 200+ `ValidationAttribute` subclasses.

**Option B: Separate `AsyncValidationAttribute` NOT deriving from `ValidationAttribute`**
   
  - Sync `Validator.TryValidateObject` uses `GetCustomAttributes<ValidationAttribute>()` and would silently skip it (not desired).

**Option C (chosen): `AsyncValidationAttribute` deriving from `ValidationAttribute`**
  
  - The sync `IsValid` override throws `InvalidOperationException`, forcing async callers. Since `AsyncValidationAttribute` IS-A `ValidationAttribute`, sync `Validator` still discovers it via reflection and produces a clear error.

**Option D: `IAsyncValidationAttribute` interface**
  
  - Less discoverable. Users must know to implement an interface AND inherit `ValidationAttribute`. The subclass approach is more idiomatic for DataAnnotations.

**`IAsyncValidatableObject.ValidateAsync` return type: `Task<IEnumerable<ValidationResult>>` instead of `IAsyncEnumerable<ValidationResult>`**

  - As [noted in the design gist](https://gist.github.com/halter73/f4d0974da579fb78d17bd2e6d9f78173), `IAsyncEnumerable<>` allows streaming results, but the alternative `Task<IEnumerable<ValidationResult>>` is simpler and may be sufficient for most scenarios. Streaming is most beneficial for progressive UI display (e.g., Blazor forms showing errors as they arrive), but many consumers will simply `await` all results at once. `Task<IEnumerable<>>` avoids the `IAsyncEnumerable` dependency and is easier to implement for attribute authors who only need a single async check. The current proposal chooses `IAsyncEnumerable` for flexibility, but this simpler alternative remains viable if streaming is deemed unnecessary for V1.

### Notes/Risks

- The new `Validator.*Async` methods follow the established `XAsync` naming pattern with distinct signatures (return `ValueTask`). No ambiguity with existing sync methods. All additions are additive, no existing APIs changed.
- Sync `Validator.TryValidateObject` discovering an `AsyncValidationAttribute` will throw `InvalidOperationException` instead of silently succeeding. This is **by design**: it surfaces the mismatch between sync callers and async-only attributes.
- Async validators run concurrently across properties and in parallel per property. If any sync attribute fails, async attributes on that property are skipped (no wasted I/O). Validators must not rely on execution order and must be safe for concurrent execution.
- **`ValueTask` rationale:** All async validation APIs return `ValueTask<T>` (or `ValueTask` for throwing variants). `IsValidAsync` and `GetValidationResultAsync` are leaf APIs called once per attribute per value — `ValueTask` avoids a `Task` allocation when validators complete synchronously (e.g., cached lookups). `Validator.TryValidateObjectAsync` and related methods are infrastructure APIs consumed via a single `await` by most callers; orchestration layers (source generator, Options startup) use `.AsTask()` for `Task.WhenAll` composition internally. See [analysis](https://gist.github.com/ViveliDuCh/df89b638cb3c91a8e758c7619f8f4620).
- **Scope:** This proposal covers the core `System.ComponentModel.DataAnnotations` APIs (Phase 1). Downstream consumers (M.E.Validation, Blazor, Options, MVC) adopt independently per the [design gist](https://gist.github.com/halter73/f4d0974da579fb78d17bd2e6d9f78173) and [integration point analysis](https://github.com/jeffhandley/dataannotations-validation/blob/main/appendices/appendix-a-integration-points.md). MVC is explicitly deferred; sync-only consumers that encounter async-only attributes get a clear error directing them to the async APIs.

### Resolved Items

1. **`IAsyncValidatableObject.ValidateAsync` return type:** Uses `IAsyncEnumerable<ValidationResult>` (streaming). Enables progressive UI display in Blazor and component vendor ecosystems. See [comparison assessment](https://github.com/ViveliDuCh/runtime/blob/async-validation-iasyncenumerable/src/libraries/System.ComponentModel.Annotations/samples/Benchmarks/COMPARISON.md).
2. **`GetValidationResultAsync` placement:** Confirmed on `AsyncValidationAttribute` only, not on base `ValidationAttribute`. The `Validator` handles `is AsyncValidationAttribute` dispatch internally.
3. **`ValidationContext.Items` thread safety:** `Items` is a read-only input channel by design. No built-in attribute mutates it during validation. The pipeline does not guarantee attribute execution order beyond `RequiredAttribute` priority. Custom validators should treat `Items` as read-only; mutations during validation are unsupported. Documented via XML `<remarks>` on `ValidationContext.Items`.
4. **Cross-property short-circuit semantics:** When `validationResults` is `null` (`breakOnFirstError=true`), the first property to complete with errors triggers cooperative cancellation of remaining in-flight async validators via linked `CancellationToken`. When `validationResults` is non-null, all properties complete and all errors are collected. Per-property sync-first gating is unconditional.

### Open Questions

1. `ValueTask<T>` vs `Task<T>` for async validation methods — leaning `ValueTask`, acknowledged trade-off


---

## Next Steps

Additional API proposals will build on top of this one but do not block this step. Expected follow-up APIs include messages, helper APIs, and progressive validation support.

#### UX-Related API Gaps

- **Pre-validation rule descriptions**: Validation attributes should be able to define/return a message describing the validation rule *before* execution, so UI can show rules upfront.
- **"Validation in progress" messaging**: Async validation attributes should provide a message while validation is running, so UX can indicate pending state. *Note: Adding more message-related APIs could make validation attribute declarations overly verbose, these concerns are linked.*
- **Detecting presence of async validators**: Possible need for an API to quickly determine whether any async validators are involved, enabling frameworks to choose between sync and async UX paths. *Noted as lower priority and possibly deferrable.*
- **Progressive validation / partial results**: Further API will likely be needed after this proposal for more progressive execution (e.g., `IProgress<ValidationResult>` callback, or `IAsyncEnumerable` return type).

#### Localization Considerations

- With parallel execution now implemented, **thread safety of localization must be considered**.
- Reuse existing `ErrorMessage` localization patterns (string vs resource-based) for consistency.
- Since async validators run concurrently, error message formatting and resource access must be thread-safe. The existing `FormatErrorMessage` pattern is safe (stateless string formatting), but custom validators that access shared mutable state during error message construction must synchronize.

