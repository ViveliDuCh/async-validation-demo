### Background and motivation

The Options validation pipeline (`IValidateOptions<T>` → `DataAnnotationValidateOptions<T>` → `Validator.TryValidateObject`) is entirely synchronous. With the proposed async `Validator` APIs in `System.ComponentModel.DataAnnotations` ([companion proposal](https://github.com/dotnet/runtime/issues/128096)), the core validation engine can await I/O-bound validation, but the Options pipeline can't consume it because every layer is sync:

```
IOptions<T>.Value          ← property (can't be async)
  → OptionsFactory.Create() ← sync method
    → IValidateOptions<T>.Validate() ← sync interface
      → Validator.TryValidateObject() ← sync (would have async counterpart)
```

**Concrete scenario:** An ASP.NET Core app validates database connection strings at startup using `ValidateOnStart()`. Today, checking connectivity requires blocking a thread. With async DataAnnotations support (e.g., `[AsyncConnectionStringValid]`), the app needs an async startup validation path.

**Strategy: Bypass, Don't Infect.** Instead of making `OptionsFactory.Create()` async (which cascades into `IOptions<T>.Value`, a property that can't return `Task`), we create a **parallel async pipeline** that runs during `Host.StartAsync()`. The sync `Create()` path is untouched (zero breaking changes).

**Relationship to `Microsoft.Extensions.Validation`:** `Microsoft.Extensions.Validation` (Minimal API endpoint validation) and `IAsyncValidateOptions<T>` (Options startup validation) are complementary, not overlapping. The former validates request payloads at runtime; the latter validates configuration at startup. Both consume the same `AsyncValidationAttribute` / `Validator.TryValidateObjectAsync()` APIs from the [companion proposal](https://github.com/dotnet/runtime/issues/128096). 
- The [aspnetcore prototype](https://github.com/ViveliDuCh/aspnetcore/tree/async-validation) demonstrates M.E.Validation adopting `AsyncValidationAttribute` with ~20 lines of changes, and Minimal APIs inheriting async support with zero additional changes.  **Prior art:** The [`oroztocil/validation-demo`](https://github.com/dotnet/aspnetcore/tree/oroztocil/validation-demo) branch in `dotnet/aspnetcore` prototyped `AsyncValidationAttribute` and `IAsyncValidatableObject` in `Microsoft.Extensions.Validation` to prove the pipeline could handle async. 

**Notable consumer:** .NET Aspire is a significant and growing consumer of `ValidateDataAnnotations()` + `ValidateOnStart()`. Any changes to `Options.DataAnnotations` or the Options validation source generator directly affect the Aspire developer experience. The async counterparts (`ValidateDataAnnotationsAsync()` + `ValidateOnStartAsync()`) benefit Aspire immediately.

**Design doc:** [Halter's consolidated requirements gist](https://gist.github.com/halter73/f4d0974da579fb78d17bd2e6d9f78173)

Related: [dotnet/aspnetcore#46349](https://github.com/dotnet/aspnetcore/issues/46349)

### API Proposal

### Microsoft.Extensions.Options

```diff
  namespace Microsoft.Extensions.Options;

+ // New interface: async counterpart to IValidateOptions<T>
+ public partial interface IAsyncValidateOptions<in TOptions> where TOptions : class
+ {
+     ValueTask<ValidateOptionsResult> ValidateAsync(
+         string? name,
+         TOptions options,
+         CancellationToken cancellationToken = default);
+ }

+ // New interface: async counterpart to IStartupValidator
+ public partial interface IAsyncStartupValidator
+ {
+     Task ValidateAsync(CancellationToken cancellationToken = default);
+ }

+ // New class: async lambda-based validator (0 dependencies)
+ public partial class AsyncValidateOptions<TOptions> : IAsyncValidateOptions<TOptions>
+     where TOptions : class
+ {
+     public AsyncValidateOptions(string? name,
+         Func<TOptions, CancellationToken, ValueTask<bool>> validation,
+         string failureMessage);
+     public string? Name { get; }
+     public Func<TOptions, CancellationToken, ValueTask<bool>> Validation { get; }
+     public string FailureMessage { get; }
+     public ValueTask<ValidateOptionsResult> ValidateAsync(
+         string? name, TOptions options, CancellationToken cancellationToken = default);
+ }

+ // New class: async lambda-based validator (1 dependency)
+ public partial class AsyncValidateOptions<TOptions, TDep> : IAsyncValidateOptions<TOptions>
+     where TOptions : class
+ {
+     public AsyncValidateOptions(string? name, TDep dependency,
+         Func<TOptions, TDep, CancellationToken, ValueTask<bool>> validation,
+         string failureMessage);
+     public string? Name { get; }
+     public TDep Dependency { get; }
+     public Func<TOptions, TDep, CancellationToken, ValueTask<bool>> Validation { get; }
+     public string FailureMessage { get; }
+     public ValueTask<ValidateOptionsResult> ValidateAsync(
+         string? name, TOptions options, CancellationToken cancellationToken = default);
+ }

+ // ... AsyncValidateOptions<TOptions, TDep1, TDep2> through <TOptions, TDep1..TDep5>
+ // (same pattern as existing sync ValidateOptions<T, TDep1..TDep5>)

  // Existing OptionsBuilderExtensions
  public static partial class OptionsBuilderExtensions
  {
      public static OptionsBuilder<TOptions> ValidateOnStart<TOptions>(this OptionsBuilder<TOptions> optionsBuilder);

+     public static OptionsBuilder<TOptions> ValidateOnStartAsync<TOptions>(
+         this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class;
  }

+ // New extension methods for async lambda validation on OptionsBuilder<T>
+ public static partial class OptionsBuilderAsyncValidationExtensions
+ {
+     // 0 dependencies
+     public static OptionsBuilder<TOptions> ValidateAsync<TOptions>(
+         this OptionsBuilder<TOptions> optionsBuilder,
+         Func<TOptions, CancellationToken, ValueTask<bool>> validation,
+         string failureMessage) where TOptions : class;
+
+     // 1 dependency
+     public static OptionsBuilder<TOptions> ValidateAsync<TOptions, TDep>(
+         this OptionsBuilder<TOptions> optionsBuilder,
+         Func<TOptions, TDep, CancellationToken, ValueTask<bool>> validation,
+         string failureMessage) where TOptions : class where TDep : notnull;
+
+     // ... up to 5 dependencies (same pattern as sync Validate<T, TDep1..TDep5>)
+ }
```

### Microsoft.Extensions.Options.DataAnnotations

```diff
  namespace Microsoft.Extensions.Options;

+ // New class: async counterpart to DataAnnotationValidateOptions<T>
+ public partial class DataAnnotationValidateOptionsAsync<TOptions>
+     : IAsyncValidateOptions<TOptions> where TOptions : class
+ {
+     public DataAnnotationValidateOptionsAsync(string? name);
+     public string? Name { get; }
+     public ValueTask<ValidateOptionsResult> ValidateAsync(
+         string? name, TOptions options, CancellationToken cancellationToken = default);
+ }

  // Existing OptionsBuilderDataAnnotationsExtensions
  public static partial class OptionsBuilderDataAnnotationsExtensions
  {
      public static OptionsBuilder<TOptions> ValidateDataAnnotations<TOptions>(this OptionsBuilder<TOptions> optionsBuilder);

+     public static OptionsBuilder<TOptions> ValidateDataAnnotationsAsync<TOptions>(
+         this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class;
  }
```

### Options Validation Source Generator

When the validator type explicitly implements `IAsyncValidateOptions<T>`, the `[OptionsValidator]` source generator emits a `ValidateAsync()` method alongside the existing sync `Validate()`. The generated code uses `Validator.TryValidateValueAsync()` per member and `Task.WhenAll` for types with 2+ validated members.

```csharp
// Source generator emits this when the validator implements IAsyncValidateOptions<TOptions>:

// Generated ValidateAsync, parallel path (2+ members with validation attributes)
public async ValueTask<ValidateOptionsResult> ValidateAsync(
    string? name, MyOptions options, CancellationToken cancellationToken = default)
{
    ValidateOptionsResultBuilder? builder = null;

    // Per-member local async functions started concurrently
    var memberTasks = new Task<List<ValidationResult>?>[2];
    memberTasks[0] = ValidateMember_ConnectionStringAsync();
    memberTasks[1] = ValidateMember_TimeoutSecondsAsync();

    List<ValidationResult>?[] memberResults =
        await Task.WhenAll(memberTasks).ConfigureAwait(false);

    foreach (List<ValidationResult>? memberResult in memberResults)
    {
        if (memberResult is not null)
        {
            (builder ??= new()).AddResults(memberResult);
        }
    }

    // IAsyncValidatableObject self-validation (if model implements it)
    // context.MemberName = "ValidateAsync";
    // (builder ??= new()).AddResults(
    //     await ((IAsyncValidatableObject)options).ValidateAsync(context, cancellationToken));

    return builder is null ? ValidateOptionsResult.Success : builder.Build();

    // Local async function per member (uses TryValidateValueAsync)
    async Task<List<ValidationResult>?> ValidateMember_ConnectionStringAsync()
    {
        var memberContext = new ValidationContext(options, "MyOptions", null, null);
        memberContext.MemberName = "ConnectionString";
        memberContext.DisplayName = string.IsNullOrEmpty(name)
            ? "ConnectionString" : $"{name}.ConnectionString";
        var validationResults = new List<ValidationResult>();
        var validationAttributes = new List<ValidationAttribute> { /* static instances */ };
        if (!await Validator.TryValidateValueAsync(
            options.ConnectionString, memberContext, validationResults,
            validationAttributes, cancellationToken).ConfigureAwait(false))
        {
            return validationResults;
        }
        return null;
    }

    // ... ValidateMember_TimeoutSecondsAsync() follows same pattern ...
}

// Generated ValidateAsync, sequential path (0 or 1 members)
// Uses the same TryValidateValueAsync but without Task.WhenAll overhead.
```

**Source generator behavior:**
- `[OptionsValidator]` on a type implementing `IAsyncValidateOptions<T>` → emits `ValidateAsync()`
- `[OptionsValidator]` on a type implementing only `IValidateOptions<T>` → emits `Validate()` only (unchanged)
- Types with `IAsyncValidatableObject` → generated `ValidateAsync()` calls `IAsyncValidatableObject.ValidateAsync()` for self-validation
- Types with `IValidatableObject` only → generated `ValidateAsync()` falls back to sync `IValidatableObject.Validate()`

Prototype: https://github.com/ViveliDuCh/runtime/tree/async-validation

### API Usage

### Scenario 1: Async DataAnnotations at startup (bypass approach)

```csharp
public class TenantDatabaseSettings
{
    [Required]
    public string TenantName { get; set; } = "";

    [Required]
    [AsyncConnectionStringValid] // AsyncValidationAttribute: tests DB connectivity
    public string ConnectionString { get; set; } = "";

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 60;
}

// In Program.cs:
builder.Services.AddOptions<TenantDatabaseSettings>()
    .Bind(builder.Configuration.GetSection("Database"))
    .ValidateDataAnnotationsAsync()   // registers IAsyncValidateOptions<T>
    .ValidateOnStartAsync();          // runs async validation at startup

// What happens:
// Host.StartAsync()
//   → IAsyncStartupValidator.ValidateAsync(ct)
//     → All registered options types validated in parallel
//     → DataAnnotationValidateOptionsAsync<TenantDatabaseSettings>.ValidateAsync()
//       → Validator.TryValidateObjectAsync(settings, ctx, results, true, ct)
//         → All properties validated in parallel
//         → Per property, Phase 1: [Required], [Range] run synchronously
//         → Per property, Phase 2: [AsyncConnectionStringValid] runs asynchronously
//       → OptionsValidationException if validation fails → app won't start
```

### Scenario 2: Async lambda-based validation with DI

```csharp
builder.Services.AddOptions<CloudInfoOptions>()
    .BindConfiguration("CloudInfo")
    .ValidateAsync<IStorageService>(async (opts, storageService, ct) =>
        await storageService.ExistsAsync(opts.Storage, ct),
        "Storage endpoint does not exist.")
    .ValidateOnStartAsync();
```

### Scenario 3: Implementing IAsyncValidateOptions<T> directly

For complex async validation with DI, implementing the interface directly is cleaner than a lambda:

```csharp
public class CloudInfoValidator : IAsyncValidateOptions<CloudInfoOptions>
{
    private readonly IStorageService _storage;

    public CloudInfoValidator(IStorageService storage) => _storage = storage;

    public async ValueTask<ValidateOptionsResult> ValidateAsync(
        string? name, CloudInfoOptions options, CancellationToken ct)
    {
        if (!await _storage.ExistsAsync(options.Storage, ct))
            return ValidateOptionsResult.Fail(
                $"Storage '{options.Storage}' not found in '{options.Region}'.");
        return ValidateOptionsResult.Success;
    }
}

// Registration:
builder.Services.AddSingleton<IAsyncValidateOptions<CloudInfoOptions>, CloudInfoValidator>();
builder.Services.AddOptions<CloudInfoOptions>()
    .BindConfiguration("CloudInfo")
    .ValidateOnStartAsync();
```

### Scenario 4: Mixed sync + async validation

Sync and async pipelines coexist. Sync validators run inside `OptionsFactory.Create()` as usual; async validators run during `Host.StartAsync()` via the bypass pipeline.

```csharp
builder.Services.AddOptions<SmtpSettings>()
    .Bind(builder.Configuration.GetSection("Smtp"))
    .ValidateDataAnnotations()        // sync [Required], [Range], runs in Create()
    .ValidateDataAnnotationsAsync()   // async [AsyncSmtpReachable], runs at startup
    .Validate(opts => opts.Port > 0,  // sync lambda
        "Port must be positive.")
    .ValidateAsync(async (opts, ct) =>// async lambda
    {
        await Task.Delay(10, ct);
        return opts.Host != "localhost" || opts.Port != 25;
    }, "Default SMTP config not allowed in production.")
    .ValidateOnStart()                // triggers sync validators
    .ValidateOnStartAsync();          // triggers async validators
```



### Alternative Designs

### `CreateAsync()` approach: making `OptionsFactory.Create()` itself async

Instead of bypassing the factory, make the factory's validation step async. This would require:

| File | Change | Difficulty |
|------|--------|-----------|
| `IOptionsFactory.cs` | New `IAsyncOptionsFactory<T>` interface with `ValueTask<T> CreateAsync()` | Easy |
| `OptionsFactory.cs` | Add `CreateAsync()`, new constructor param for `IAsyncValidateOptions<T>[]` | Medium |
| `UnnamedOptionsManager.cs` | Replace `lock` with `SemaphoreSlim`, add sync fallback | Hard |
| `OptionsManager.cs` | Add `GetAsync()`, deal with sync `ConcurrentDictionary<Lazy<T>>` cache | Hard |
| `OptionsMonitor.cs` | Add `GetAsync()`, async change notification | Hard |
| `OptionsCache.cs` | Need `AsyncLazy<T>` (doesn't exist in BCL) | Hard |
| `IOptions<T>.Value` | Property, can't return `Task` | **Impossible** |

**Why not chosen:**
- `IOptions<T>.Value` is a property consumed by every ASP.NET Core app, middleware, and third-party library. It cannot be made async.
- `lock` + `await` is prohibited in C#, so `UnnamedOptionsManager` would need a complete rewrite.
- `ConcurrentDictionary<Lazy<T>>` has no async `GetOrAdd`, so the entire cache primitive would need replacement.
- Every sync consumer of `OptionsFactory` would need a sync-over-async fallback (`.GetAwaiter().GetResult()`), creating deadlock risk in ASP.NET contexts.

The **bypass approach** avoids all of these problems by separating creation from validation. `OptionsFactory.Create()` runs normally (configure + post-configure, no async validators registered), and the async pipeline validates the resulting cached instance during `Host.StartAsync()`.

### Risks

- All additions are additive: new interfaces, new classes, new extension methods. No overload ambiguity, `ValidateDataAnnotationsAsync` and `ValidateOnStartAsync` have distinct names from their sync counterparts.
- `ValidateDataAnnotationsAsync` registers ONLY `IAsyncValidateOptions<T>`, not `IValidateOptions<T>`. This means sync `OptionsFactory.Create()` will NOT run async-only attributes. This is by design: async attributes should only run in the async pipeline. Developers using `ValidateDataAnnotationsAsync` without `ValidateOnStartAsync` will get no validation of async attributes, the API docs should make this pairing clear.
- **Timing difference:** Sync `ValidateOnStart` validates during `OptionsFactory.Create()` (inside the first `Get()` call). Async `ValidateOnStartAsync` validates in a separate step after `Create()`. Both run during `Host.StartAsync()` before any request: the end user behavior is identical (invalid config → app crashes at startup).
- **Parallel execution:** All async validation runs in parallel at multiple levels (see below). Validators must not rely on execution order and must be safe for concurrent invocation.

## Parallel Execution Model

The async Options validation pipeline executes validators in parallel at four levels to maximize throughput for I/O-bound validators:

### 1. Cross-options-type parallelism (`AsyncStartupValidator.ValidateAsync`)

When multiple options types are registered with `ValidateOnStartAsync()`, all are validated concurrently:

```csharp
// These two options types validate in parallel at startup
services.AddOptions<DatabaseSettings>()
    .ValidateDataAnnotationsAsync()
    .ValidateOnStartAsync();

services.AddOptions<CacheSettings>()
    .ValidateDataAnnotationsAsync()
    .ValidateOnStartAsync();

// Host.StartAsync() → IAsyncStartupValidator.ValidateAsync()
//   → DatabaseSettings validation ─┐
//   → CacheSettings validation  ───┤── run concurrently
//   → collect all OptionsValidationExceptions
//   → single exception: rethrow; multiple: AggregateException
```

### 2. Cross-validator parallelism (`ValidateOnStartAsync` lambda)

When multiple `IAsyncValidateOptions<T>` validators are registered for the same options type (via `.ValidateAsync()` lambdas, `IAsyncValidateOptions<T>` implementations, or `ValidateDataAnnotationsAsync()`), all validators run concurrently:

```csharp
services.AddOptions<CloudInfoOptions>()
    .ValidateAsync(async (opts, ct) =>
        await CheckStorageAsync(opts.Storage, ct), "Storage not found")
    .ValidateAsync(async (opts, ct) =>
        await CheckRegionAsync(opts.Region, ct), "Region not found")
    .ValidateOnStartAsync();

// Both validators start concurrently via Task.WhenAll
// All failures collected after both complete
```

### 3. Nested property parallelism (`DataAnnotationValidateOptionsAsync`)

For options types with `[ValidateObjectMembers]` or `[ValidateEnumeratedItems]` attributes, nested object validation runs in parallel. Each nested object gets its own validation context and visited-set snapshot:

```csharp
public class AppSettings
{
    [ValidateObjectMembers]
    public DatabaseSettings Database { get; set; }

    [ValidateObjectMembers]
    public CacheSettings Cache { get; set; }
}

// Database and Cache validated concurrently via Task.WhenAll
```

### 4. Source generator parallelism (Options validation source generator)

When the Options validation source generator emits code for types implementing `IAsyncValidateOptions<T>` with 2+ members having validation attributes, the generated `ValidateAsync` method validates members in parallel using per-member local async functions and `Task.WhenAll`:

```csharp
// Generated code (simplified):
public async ValueTask<ValidateOptionsResult> ValidateAsync(
    string? name, MyOptions options, CancellationToken cancellationToken)
{
    var memberTasks = new Task<List<ValidationResult>?>[2];
    memberTasks[0] = ValidateMember_ConnectionStringAsync();
    memberTasks[1] = ValidateMember_TimeoutAsync();

    var results = await Task.WhenAll(memberTasks).ConfigureAwait(false);
    // ... collect results ...
}
```

For types with 0 or 1 validated members, the sequential path is preserved (no parallelization overhead).

### Behavioral changes from parallelism

| Aspect | Before (Sequential) | After (Parallel) |
|--------|---------------------|-------------------|
| Validator execution order | Deterministic (registration order) | Non-deterministic |
| Failure collection | All failures collected (no short-circuit) | All failures collected ✅ same |
| Unexpected exceptions | Stops at first throw | All validators run; exceptions collected after |
| Cancellation | Each validator gets the same token | Same ✅ |
| Thread safety | Single-threaded within pipeline | Validators must be safe for concurrent execution |
