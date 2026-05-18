# Options Async Validation Samples

Demonstrates the async bypass pipeline for `IOptions<T>` startup validation
([dotnet/runtime#128100](https://github.com/dotnet/runtime/issues/128100)).
All samples use the **local-packages** DLLs built from the
[`async-validation` branch](https://github.com/ViveliDuCh/runtime/tree/async-validation) of `dotnet/runtime`.

## Scenario Coverage Matrix

| Issue Scenario | Description | Pattern | Sample |
|---------------|-------------|---------|--------|
| **1** | Async DataAnnotations at startup | Reflection | [`Tier2.OptionsBlazor`](BlazorSamples/Tier2.OptionsBlazor/) |
| **1** | Async DataAnnotations + `OptionsMonitor` reload/revalidation | Reflection | [`Tier2.OptionsMonitorBlazor`](BlazorSamples/Tier2.OptionsMonitorBlazor/) |
| **2** | Async lambda with DI dependency | Reflection | [`AsyncLambdaConsole`](ConsoleAppSamples/AsyncLambdaConsole/) |
| **3** | Source generator `[OptionsValidator]` + `IAsyncValidateOptions<T>` | Source Gen | [`Tier2b.OptionsGeneratorBlazor`](BlazorSamples/Tier2b.OptionsGeneratorBlazor/) |
| **4** | Mixed sync + async on the same `OptionsBuilder` + nested `[ValidateObjectMembers]` | Reflection | [`MixedSyncAsyncConsole`](ConsoleAppSamples/MixedSyncAsyncConsole/) |
| **5** | Sync pipeline hits async-only `AsyncStorageExistsAttribute` and throws `NotSupportedException`; fix is to switch to the async pipeline | Both | [`SyncFallbackConsole`](ConsoleAppSamples/SyncFallbackConsole/) |
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
| Source generator member parallelism | Source Gen | [`Tier2b.OptionsGeneratorBlazor`](BlazorSamples/Tier2b.OptionsGeneratorBlazor/) |

> **Scenario 6 is reflection-only.** Reflection-based `Validator.TryValidateObjectAsync()` runs all sync attributes across the object before it schedules any async attributes, so a sync failure on one property short-circuits async work everywhere. Source-generated validators use per-property `TryValidateValueAsync()`, so a sync failure on one property does **not** stop async checks on other properties.



## How the Bypass Pipeline Works

The async Options validation uses a **bypass** design: instead of making
`OptionsFactory.Create()` async (which would cascade into `IOptions<T>.Value`,
a property), a parallel async pipeline runs during `Host.StartAsync()`.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Host.StartAsync()                            │
│                                                                     │
│  ┌──────────────────────────────┐  ┌─────────────────────────────┐  │
│  │     SYNC PATH (existing)     │  │    ASYNC PATH (new bypass)  │  │
│  │                              │  │                             │  │
│  │  IOptions<T>.Value           │  │  IAsyncStartupValidator     │  │
│  │    → OptionsFactory.Create() │  │    .ValidateAsync()         │  │
│  │      → IValidateOptions<T>   │  │      → IAsyncValidateOptions│  │
│  │        .Validate()           │  │        .ValidateAsync()     │  │
│  │      → Validator             │  │      → Validator            │  │
│  │        .TryValidateObject()  │  │        .TryValidateObject   │  │
│  │                              │  │         Async()             │  │
│  │  Triggered by:               │  │  Triggered by:              │  │
│  │  · ValidateDataAnnotations() │  │  · ValidateDataAnnotations  │  │
│  │  · Validate(lambda)          │  │     Async()                 │  │
│  │  · ValidateOnStart()         │  │  · ValidateAsync(lambda)    │  │
│  └──────────────────────────────┘  │  · ValidateOnStartAsync()   │  │
│                                    └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
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
> (async, non-blocking) and `IsValid()` (sync fallback, blocking). This is the
> recommended pattern when mixing `ValidateDataAnnotations()` + `ValidateDataAnnotationsAsync()`
> on the same model — the sync path calls `IsValid()` and the async path calls `IsValidAsync()`.

#### Registration (mixed sync + async)

```csharp
builder.Services.AddOptions<SmtpSettings>()
    .Bind(config.GetSection("Smtp"))
    .ValidateDataAnnotations()        // sync [Required], [Range], [AsyncSmtpReachable] sync fallback
    .ValidateDataAnnotationsAsync()   // async [AsyncSmtpReachable] non-blocking,
                                      // nested [ValidateObjectMembers]
    .Validate(opts => opts.Port > 0,
        "Port must be positive.")                        // sync lambda
    .ValidateAsync(async (opts, ct) =>
    {
        await Task.CompletedTask;
        return opts.Host != "localhost" || opts.Port != 25;
    }, "Default SMTP config not allowed in production.") // async lambda
    .ValidateOnStart()                // triggers sync validators in Create()
    .ValidateOnStartAsync();          // triggers async validators at startup
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
  └── Async path (ValidateOnStartAsync → IAsyncStartupValidator):
        ┌──────────────────────────────────────┐
        │ ValidateDataAnnotationsAsync runs     │
        │ Validator.TryValidateObjectAsync:     │
        │                                       │
        │   Top-level:                          │
        │     [AsyncSmtpReachable]              │
        │       .IsValidAsync() Host ─────┐     │
        │                                 │     │
        │   Nested [ValidateObjectMembers]:│     │
        │     Credentials ────────────────┤     │
        │       [Required] Username       │     │
        │       [Required] Password       ├─ parallel
        │       [MinLength] Password      │     │
        │                                 │     │
        │   ValidateAsync(lambda) ────────┘     │
        │     localhost:25 check                │
        └──────────────────────────────────────┘
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
`ValidateOnStartAsync()`. At startup, `IAsyncStartupValidator` validates both
concurrently via `Task.WhenAll`.

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
    .ValidateDataAnnotationsAsync()
    .ValidateOnStartAsync();

builder.Services.AddOptions<CacheSettings>()
    .BindConfiguration("Cache")
    .ValidateDataAnnotationsAsync()
    .ValidateOnStartAsync();
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

- **Reflection path:** `ValidateDataAnnotations()` + `ValidateOnStart()` calls
  `Validator.TryValidateObject()`, which reaches `IsValid()` and throws
  `NotSupportedException`.
- **Source-gen path:** registering `CloudInfoOptionsValidator` as
  `IValidateOptions<T>` makes generated `Validate()` call `IsValid()` too, which
  throws the same way.
- **Fix:** switch to the async pipeline:
  - reflection → `ValidateDataAnnotationsAsync()` + `ValidateOnStartAsync()`
  - source gen → register `CloudInfoOptionsValidator` as
    `IAsyncValidateOptions<T>` + `ValidateOnStartAsync()`

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

## Folder Structure

```
Options/
├── Options.Shared/                          ← Shared library (stays at top)
├── ConsoleAppSamples/
│   ├── AsyncLambdaConsole/                  ← Scenario 2: `.ValidateAsync<TDep>(lambda)`
│   ├── MixedSyncAsyncConsole/               ← Scenario 4 + reflection nested `[ValidateObjectMembers]`
│   ├── CrossTypeParallelConsole/            ← Reflection cross-options-type parallel startup validation
│   ├── SyncFallbackConsole/                 ← Sync path hits async-only attr; async pipeline fix
│   └── SourceGenScenariosConsole/           ← Source-gen scenario pack + reflection contrast
├── BlazorSamples/
│   ├── Tier2.OptionsBlazor/                 ← Scenario 1: `ValidateDataAnnotationsAsync()`
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

Push-Location src\Options\BlazorSamples\Tier2.OptionsBlazor; dotnet run; Pop-Location
Push-Location src\Options\BlazorSamples\Tier2.OptionsMonitorBlazor; dotnet run; Pop-Location
Push-Location src\Options\BlazorSamples\Tier2b.OptionsGeneratorBlazor; dotnet run; Pop-Location
```
