# Options Async Validation Samples

Demonstrates the async bypass pipeline for `IOptions<T>` startup validation
([dotnet/runtime#128100](https://github.com/dotnet/runtime/issues/128100),
merged via [dotnet/runtime#128788](https://github.com/dotnet/runtime/pull/128788)
and the DataAnnotations bridge [dotnet/runtime#129218](https://github.com/dotnet/runtime/pull/129218)).
All samples use the **local-packages** DLLs built from the
[`async-validation` branch](https://github.com/ViveliDuCh/runtime/tree/async-validation) of `dotnet/runtime`.

> ### ⚠️ Merged API delta vs. the prototype prose below
>
> The merged shipping API uses fewer methods than the prototype. When reading
> the rest of this document, mentally apply the following translation:
>
> | Prototype (older prose) | Merged (shipping) |
> |---|---|
> | `ValidateDataAnnotationsAsync()` | `ValidateDataAnnotations()` — on .NET 11+ this single call registers **both** `IValidateOptions<T>` and `IAsyncValidateOptions<T>` from one shared `DataAnnotationValidateOptions<T>`. |
> | `ValidateOnStartAsync()` | `ValidateOnStart()` — single method drives both `IStartupValidator` (sync) and `IAsyncStartupValidator` (async) when both are registered. |
> | `OptionsBuilder<T>.ValidateAsync(async lambda, …)` | `OptionsBuilder<T>.Validate(async lambda, …)` — async is now an overload of the existing `Validate(…)` method. |
> | `Task<ValidationResult?>` returned from `IsValidAsync` | **same** — merged API returns `Task<>`, not `ValueTask<>`. |
> | `NotSupportedException` thrown by sync fallback | `InvalidOperationException` (matches merged XML docs). |

## Scenario Coverage Matrix

| Issue Scenario | Description | Pattern | Sample |
|---------------|-------------|---------|--------|
| **1** | Async DataAnnotations at startup | Reflection | [`Tier2.OptionsBlazor`](BlazorSamples/Tier2.OptionsBlazor/) |
| **1** | Async DataAnnotations + `OptionsMonitor` reload/revalidation | Reflection | [`Tier2.OptionsMonitorBlazor`](BlazorSamples/Tier2.OptionsMonitorBlazor/) |
| **2** | Async lambda with DI dependency | Reflection | [`AsyncLambdaConsole`](ConsoleAppSamples/AsyncLambdaConsole/) |
| **3** | Source generator `[OptionsValidator]` + `IAsyncValidateOptions<T>` | Source Gen | [`Tier2b.OptionsGeneratorBlazor`](BlazorSamples/Tier2b.OptionsGeneratorBlazor/) |
| **4** | Mixed sync + async on the same `OptionsBuilder` + nested `[ValidateObjectMembers]` | Reflection | [`MixedSyncAsyncConsole`](ConsoleAppSamples/MixedSyncAsyncConsole/) |
| **5** | Sync pipeline hits async-only `AsyncStorageExistsAttribute` and throws `InvalidOperationException`; fix is to switch to the async pipeline | Both | [`SyncFallbackConsole`](ConsoleAppSamples/SyncFallbackConsole/) |
| **6** | Cross-property two-phase short-circuit: a sync failure anywhere in object validation prevents async attrs from running | Reflection | [`MixedSyncAsyncConsole`](ConsoleAppSamples/MixedSyncAsyncConsole/) |
| **6 (contrast)** | Source-gen per-property validation has no cross-property short-circuit; async checks on other properties still run | Source Gen | [`SourceGenScenariosConsole`](ConsoleAppSamples/SourceGenScenariosConsole/) |
| **7** | Source-gen scenario pack: mixed sync+async attrs, dual-mode attr, same-property two-phase, cross-type parallel, nested members, and startup failure | Source Gen | [`SourceGenScenariosConsole`](ConsoleAppSamples/SourceGenScenariosConsole/) |

| Advanced Pattern | Pattern | Sample |
|------------------|---------|--------|
| Cross-options-type (2+ types at startup) | Reflection | [`CrossTypeParallelConsole`](ConsoleAppSamples/CrossTypeParallelConsole/) |
| Cross-options-type (2+ types at startup) | Source Gen | [`SourceGenScenariosConsole`](ConsoleAppSamples/SourceGenScenariosConsole/) |
| Cross-validator (chained lambdas) | Reflection | [`AsyncLambdaConsole`](ConsoleAppSamples/AsyncLambdaConsole/) |
| Nested property (`[ValidateObjectMembers]`) | Reflection | [`MixedSyncAsyncConsole`](ConsoleAppSamples/MixedSyncAsyncConsole/) |
| Nested property (`[ValidateObjectMembers]`) | Source Gen | [`SourceGenScenariosConsole`](ConsoleAppSamples/SourceGenScenariosConsole/) |
| Transitive validation (nested + collection + circular) | Reflection | [`TransitiveValidationConsole`](ConsoleAppSamples/TransitiveValidationConsole/) |
| Source generator member parallelism | Source Gen | [`Tier2b.OptionsGeneratorBlazor`](BlazorSamples/Tier2b.OptionsGeneratorBlazor/) |

> **Scenario 6 is reflection-only.** Reflection-based `Validator.TryValidateObjectAsync()` runs all sync attributes across the object before it schedules any async attributes, so a sync failure on one property short-circuits async work everywhere. Source-generated validators use per-property `TryValidateValueAsync()`, so a sync failure on one property does **not** stop async checks on other properties.



## How the Bypass Pipeline Works

The async Options validation uses a **bypass** design: instead of making
`OptionsFactory.Create()` async (which would cascade into `IOptions<T>.Value`,
a property), a parallel async pipeline runs during `Host.StartAsync()`.

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Host.StartAsync()                             │
│                                                                      │
│  ┌──────────────────────────────┐   ┌─────────────────────────────┐  │
│  │     SYNC PATH (existing)     │   │    ASYNC PATH (new bypass)  │  │
│  │                              │   │                             │  │
│  │  IOptions<T>.Value           │   │  IAsyncStartupValidator     │  │
│  │    → OptionsFactory.Create() │   │    .ValidateAsync()         │  │
│  │      → IValidateOptions<T>   │   │      → IAsyncValidateOptions│  │
│  │        .Validate()           │   │        .ValidateAsync()     │  │
│  │      → Validator             │   │      → Validator            │  │
│  │        .TryValidateObject()  │   │        .TryValidate         │  │
│  │                              │   │         ObjectAsync()       │  │
│  │  Triggered by:               │   │  Triggered by:              │  │
│  │  · ValidateDataAnnotations() │   │  · ValidateDataAnnotations()│  │
│  │  · Validate(lambda)          │   │     (same call, .NET 11+    │  │
│  │  · ValidateOnStart()         │   │      registers both)        │  │
│  │                              │   │  · Validate(async lambda)   │  │
│  │                              │   │     (overload of Validate)  │  │
│  │                              │   │  · ValidateOnStart() drives │  │
│  │                              │   │     this validator too      │  │
│  └──────────────────────────────┘   └─────────────────────────────┘  │
│                                                                      │
│  ValidateOnStart() registers BOTH IStartupValidator (sync) and       │
│  IAsyncStartupValidator (async) when an IAsyncValidateOptions<T>     │
│  is registered. There is no separate ValidateOnStartAsync().         │
└──────────────────────────────────────────────────────────────────────┘
```

## Console Sample Highlights

### MixedSyncAsyncConsole — Scenario 4 + Nested Properties

Demonstrates chaining **both** sync and async validation on the same
`OptionsBuilder`, exactly as described in Issue Scenario 4. Also shows
`[ValidateObjectMembers]` for nested property parallelism.

#### Options Model

```csharp
public class SmtpSettings
{
    [Required]                    // ← sync: [Required] works in both paths
    [AsyncSmtpReachable]          // ← dual-mode: sync fallback + async non-blocking
    public string Host { get; set; } = "";

    [Range(1, 65535)]             // ← sync
    public int Port { get; set; } = 587;

    public bool UseTls { get; set; } = true;

    [ValidateObjectMembers]       // ← nested: async pipeline validates in parallel
    public SmtpCredentials Credentials { get; set; } = new();
}

public class SmtpCredentials
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";
}
```

> **Dual-mode attributes:** `[AsyncSmtpReachable]` overrides both `IsValidAsync()`
> (async, non-blocking) and `IsValid(value, ctx)` (sync fallback, blocking). On the
> merged API, `AsyncValidationAttribute.IsValid(value, ctx)` is `protected abstract`
> — every async attribute **must** implement it. The sync pipeline calls `IsValid()`;
> the async pipeline calls `IsValidAsync()`. A single `.ValidateDataAnnotations()` chain
> on .NET 11+ wires up both, so one registration covers both pipelines.

#### Registration (mixed sync + async — merged API)

```csharp
builder.Services.AddOptions<SmtpSettings>()
    .Bind(config.GetSection("Smtp"))
    .ValidateDataAnnotations()        // .NET 11+: registers BOTH IValidateOptions<T>
                                      // and IAsyncValidateOptions<T> from one shared
                                      // DataAnnotationValidateOptions<T> instance —
                                      // covers sync attrs, async attrs, and nested
                                      // [ValidateObjectMembers] in both pipelines.
    .Validate(opts => opts.Port > 0,
        "Port must be positive.")                        // sync lambda
    .Validate(async (opts, ct) =>                        // async lambda — same Validate()
    {                                                    // method, new overload taking
        await Task.CompletedTask;                        // Func<T, CT, Task<bool>>
        return opts.Host != "localhost" || opts.Port != 25;
    }, "Default SMTP config not allowed in production.")
    .ValidateOnStart();               // single call — drives IStartupValidator AND
                                      // IAsyncStartupValidator at Host.StartAsync().
```

#### What happens at startup

```
Host.StartAsync()
  │
  ├── Sync path (ValidateOnStart → OptionsFactory.Create):
  │     [Required] Host ✓
  │     [AsyncSmtpReachable].IsValid() sync fallback ✓
  │     [Range]    Port ✓
  │     Validate(lambda) Port > 0 ✓
  │
  └── Async path (ValidateOnStart → IAsyncStartupValidator):
        ┌────────────────────────────────────────┐
        │ ValidateDataAnnotations runs            │
        │ Validator.TryValidateObjectAsync:       │
        │                                         │
        │   Top-level:                            │
        │     [AsyncSmtpReachable]                │
        │       .IsValidAsync() Host ─────┐       │
        │                                 │       │
        │   Nested [ValidateObjectMembers]:│      │
        │     Credentials ────────────────┤       │
        │       [Required] Username       │       │
        │       [Required] Password       ├─ parallel
        │       [MinLength] Password      │       │
        │                                 │       │
        │   Validate(async lambda) ───────┘       │
        │     localhost:25 check                  │
        └────────────────────────────────────────┘
```

#### Scenarios

| # | Config | Sync | Async | Result |
|---|--------|------|-------|--------|
| 1 | Valid SMTP | ✅ | ✅ | App starts |
| 2 | Port=0, short password | ❌ [Range], [MinLength] | — | Sync catches it first |
| 3 | Host=unreachable | ✅ | ❌ [AsyncSmtpReachable] | Async catches it |

---

### CrossTypeParallelConsole — Cross-Options-Type Parallelism

Demonstrates registering **two independent options types** with
`ValidateOnStart()`. On the merged API, that single call drives the
`IAsyncStartupValidator` registered by `ValidateDataAnnotations()`, and that
validator runs both options types concurrently via `Task.WhenAll`.

#### Options Models

```csharp
public class DatabaseSettings
{
    [Required]
    [AsyncConnectionReachable]    // simulates 200ms DB connectivity check
    public string ConnectionString { get; set; } = "";

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}

public class CacheSettings
{
    [Required]
    [AsyncCacheReachable]         // simulates 200ms Redis PING
    public string Endpoint { get; set; } = "";

    [Range(1, 3600)]
    public int DefaultTtlSeconds { get; set; } = 300;
}
```

#### Registration (two types, both async)

```csharp
builder.Services.AddOptions<DatabaseSettings>()
    .BindConfiguration("Database")
    .ValidateDataAnnotations()   // .NET 11+: also registers IAsyncValidateOptions<T>
    .ValidateOnStart();          // drives IAsyncStartupValidator too

builder.Services.AddOptions<CacheSettings>()
    .BindConfiguration("Cache")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

#### Parallel execution at startup

```
Host.StartAsync()
  └── IAsyncStartupValidator.ValidateAsync()
        │
        ├── DatabaseSettings ──→ [AsyncConnectionReachable] ──→ 200ms
        │                                                         │
        ├── CacheSettings ─────→ [AsyncCacheReachable] ──────→ 200ms
        │                                                         │
        └── Task.WhenAll ──────────────────────────────────→ ≈200ms total
                                                           (not 400ms)
```

Each async attribute simulates a 200ms I/O probe. The demo prints elapsed time
to show parallel execution: total ≈ 200ms (max), not 400ms (sum).

#### Scenarios

| # | Database | Cache | Result |
|---|----------|-------|--------|
| 1 | ✅ valid | ✅ valid | Both pass, ≈200ms total |
| 2 | ✅ valid | ❌ invalid | `OptionsValidationException` for cache |
| 3 | ❌ invalid | ❌ invalid | `AggregateException` wrapping both failures |

---

### SyncFallbackConsole — Sync Path Failure + Async Fix

Demonstrates what happens when an **async-only** attribute (`AsyncStorageExistsAttribute`)
is reached by a **sync** validation path.

- **Reflection sync path:** if the user calls `Validator.TryValidateObject()`
  directly (or registers only a sync `IValidateOptions<T>` that does the same),
  reaching an async-only attribute throws `InvalidOperationException`
  ("This validation attribute requires asynchronous validation. Use the async
  Validator APIs instead.").
- **Source-gen sync path:** registering `CloudInfoOptionsValidator` solely as
  `IValidateOptions<T>` makes the generated `Validate()` call `IsValid()`, which
  throws the same way.
- **Fix on merged API:** use the unified DataAnnotations chain:
  - reflection → `ValidateDataAnnotations() + ValidateOnStart()` — on .NET 11+ a
    single chain registers `IAsyncValidateOptions<T>` too, and `ValidateOnStart()`
    drives both startup validators.
  - source gen → register `CloudInfoOptionsValidator` as `IAsyncValidateOptions<T>`
    (in addition to or instead of `IValidateOptions<T>`) + `ValidateOnStart()`.

This sample explicitly covers **both** reflection and source-gen patterns.

---

### SourceGenScenariosConsole — Source-Gen Scenario Pack

Source-generated equivalents of [`MixedSyncAsyncConsole`](ConsoleAppSamples/MixedSyncAsyncConsole/)
and [`CrossTypeParallelConsole`](ConsoleAppSamples/CrossTypeParallelConsole/).
It bundles the source-gen versions of the key behaviors into one console app:

1. mixed sync + async attrs on the same property
2. dual-mode attr sync fallback
3. same-property two-phase behavior (sync fail skips async on that property)
4. cross-property non-short-circuit contrast vs reflection
5. cross-type parallel startup validation
6. nested `[ValidateObjectMembers]`
7. startup failure / `OptionsValidationException`

---

### TransitiveValidationConsole — Transitive Async Validation

Demonstrates `ValidateDataAnnotations()` (which now also registers the async
validator on .NET 11+) with the **recursive walk** that
honors `[ValidateObjectMembers]` and `[ValidateEnumeratedItems]` — the same
transitive validation the sync `DataAnnotationValidateOptions` provides, but
using `Validator.TryValidateObjectAsync` at each level.

#### What it covers

1. **[ValidateObjectMembers]** — nested `DatabaseSettings` with `[AsyncConnectionStringValid]`
2. **Multi-level nesting** — `TenantSettings → FailoverSettings → DatabaseSettings`
3. **[ValidateEnumeratedItems]** — `List<EndpointEntry>` with per-item `[AsyncEndpointHealthy]`
4. **Mixed sync + async at multiple levels** — `[Required]`/`[Range]` + async attrs
5. **Circular references** — `CircularParent ↔ CircularChild` cycle detection

#### Options Model

```
TenantSettings (top-level)
  ├── [ValidateObjectMembers] Database   → DatabaseSettings
  │     └── [AsyncConnectionStringValid] ConnectionString
  ├── [ValidateObjectMembers] Failover   → FailoverSettings
  │     └── [ValidateObjectMembers] Primary → DatabaseSettings
  └── [ValidateEnumeratedItems] Endpoints → List<EndpointEntry>
        └── [AsyncEndpointHealthy] Url
```

#### Scenarios

| # | Config | Expected |
|---|--------|----------|
| 1 | All valid | Recursive walk passes at every level |
| 2 | Database unreachable | First-level `[ValidateObjectMembers]` catches it |
| 3 | Failover DB unreachable | Multi-level recursion catches deep-nested failure |
| 4 | Unhealthy endpoint in list | `[ValidateEnumeratedItems]` catches it at index |
| 5 | Mixed failures everywhere | Sync + async errors aggregated across all levels |
| 6 | Circular Parent ↔ Child | Cycle detection prevents infinite loop |

## Folder Structure

```
Options/
├── Options.Shared/                          ← Shared library (stays at top)
├── ConsoleAppSamples/
│   ├── AsyncLambdaConsole/                  ← Scenario 2: `.ValidateAsync<TDep>(lambda)`
│   ├── MixedSyncAsyncConsole/               ← Scenario 4 + reflection nested `[ValidateObjectMembers]`
│   ├── CrossTypeParallelConsole/            ← Reflection cross-options-type parallel startup validation
│   ├── SyncFallbackConsole/                 ← Sync path hits async-only attr; async pipeline fix
│   ├── SourceGenScenariosConsole/           ← Source-gen scenario pack + reflection contrast
│   └── TransitiveValidationConsole/         ← Transitive async validation: nested + collection + circular
├── BlazorSamples/
│   ├── Tier2.OptionsBlazor/                 ← Scenario 1: `ValidateDataAnnotations()` + manual sync-then-async startup validation
│   ├── Tier2.OptionsMonitorBlazor/          ← Scenario 1 + runtime reload
│   └── Tier2b.OptionsGeneratorBlazor/       ← Scenario 3: `[OptionsValidator]` + `IAsyncValidateOptions<T>`
└── README.md
```

## Running the Samples

Set up the repo-local .NET SDK first:

```powershell
$env:DOTNET_ROOT = "C:\REPOS\async-validation-demo\.dotnet"
$env:PATH = "C:\REPOS\async-validation-demo\.dotnet;$env:PATH"
```

Run each sample from its own project directory so `appsettings.json` is resolved correctly:

```powershell
Push-Location src\Options\ConsoleAppSamples\AsyncLambdaConsole; dotnet run; Pop-Location
Push-Location src\Options\ConsoleAppSamples\MixedSyncAsyncConsole; dotnet run; Pop-Location
Push-Location src\Options\ConsoleAppSamples\CrossTypeParallelConsole; dotnet run; Pop-Location
Push-Location src\Options\ConsoleAppSamples\SyncFallbackConsole; dotnet run; Pop-Location
Push-Location src\Options\ConsoleAppSamples\SourceGenScenariosConsole; dotnet run; Pop-Location
Push-Location src\Options\ConsoleAppSamples\TransitiveValidationConsole; dotnet run; Pop-Location

Push-Location src\Options\BlazorSamples\Tier2.OptionsBlazor; dotnet run; Pop-Location
Push-Location src\Options\BlazorSamples\Tier2.OptionsMonitorBlazor; dotnet run; Pop-Location
Push-Location src\Options\BlazorSamples\Tier2b.OptionsGeneratorBlazor; dotnet run; Pop-Location
```
