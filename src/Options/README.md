# Options Async Validation Samples

Demonstrates the async bypass pipeline for `IOptions<T>` startup validation
([dotnet/runtime#128100](https://github.com/dotnet/runtime/issues/128100)).
All samples use the **local-packages** DLLs built from the
[`async-validation` branch](https://github.com/ViveliDuCh/runtime/tree/async-validation) of `dotnet/runtime`.

## Scenario Coverage Matrix

| Issue Scenario | Description | Sample |
|---------------|-------------|--------|
| **1** | Async DataAnnotations at startup | [`Tier2.OptionsBlazor`](Tier2.OptionsBlazor/) |
| **1** | Async DataAnnotations + OptionsMonitor | [`Tier2.OptionsMonitorBlazor`](Tier2.OptionsMonitorBlazor/) |
| **2** | Async lambda with DI dependency | [`AsyncLambdaConsole`](AsyncLambdaConsole/) |
| **3** | Source generator `[OptionsValidator]` + `IAsyncValidateOptions<T>` | [`Tier2b.OptionsGeneratorBlazor`](Tier2b.OptionsGeneratorBlazor/) |
| **4** | Mixed sync + async on same `OptionsBuilder` | [`MixedSyncAsyncConsole`](MixedSyncAsyncConsole/) ✨ |

| Parallel Execution Pattern | Sample |
|---------------------------|--------|
| Cross-options-type (2+ types at startup) | [`CrossTypeParallelConsole`](CrossTypeParallelConsole/) ✨ |
| Cross-validator (chained lambdas) | [`AsyncLambdaConsole`](AsyncLambdaConsole/) |
| Nested property (`[ValidateObjectMembers]`) | [`MixedSyncAsyncConsole`](MixedSyncAsyncConsole/) ✨ |
| Source generator member parallelism | [`Tier2b.OptionsGeneratorBlazor`](Tier2b.OptionsGeneratorBlazor/) |

✨ = newly added

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

## New Samples

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

## Folder Structure

```
Options/
├── Options.Shared/                  ← Shared options POCOs and async attributes
│   ├── CloudInfoOptions.cs          ← [Required] + [AsyncStorageExists]
│   └── AsyncStorageExistsAttribute  ← Simulates async endpoint probe
│
├── Tier2.OptionsBlazor/             ← Scenario 1: ValidateDataAnnotationsAsync()
├── Tier2.OptionsMonitorBlazor/      ← Scenario 1 + runtime reload
├── AsyncLambdaConsole/              ← Scenario 2: .ValidateAsync<TDep>(lambda)
├── Tier2b.OptionsGeneratorBlazor/   ← Scenario 3: [OptionsValidator] + IAsyncValidateOptions
│
├── MixedSyncAsyncConsole/        ✨ ← Scenario 4: mixed sync+async + [ValidateObjectMembers]
│   ├── SmtpSettings.cs              ← Mixed attrs + nested [ValidateObjectMembers]
│   ├── SmtpCredentials.cs           ← Nested POCO with sync attrs
│   ├── AsyncSmtpReachableAttribute  ← Simulates async SMTP check
│   └── Program.cs                   ← 3 scenarios: valid / sync fail / async fail
│
└── CrossTypeParallelConsole/     ✨ ← Cross-type parallelism with timing proof
    ├── DatabaseSettings.cs          ← [AsyncConnectionReachable] (200ms)
    ├── CacheSettings.cs             ← [AsyncCacheReachable] (200ms)
    ├── AsyncConnection/Cache attrs  ← Simulated 200ms connectivity checks
    └── Program.cs                   ← 3 scenarios: both OK / one fails / both fail
```

## Running the Samples

Each console sample runs standalone:

```powershell
# Set up the repo-local .NET SDK
$env:DOTNET_ROOT = "C:\REPOS\async-validation-demo\.dotnet"
$env:PATH = "C:\REPOS\async-validation-demo\.dotnet;$env:PATH"

# Mixed sync + async demo
dotnet run --project src/Options/MixedSyncAsyncConsole

# Cross-type parallel demo
dotnet run --project src/Options/CrossTypeParallelConsole
```

For the Blazor samples:

```powershell
dotnet run --project src/Options/Tier2.OptionsBlazor
dotnet run --project src/Options/Tier2.OptionsMonitorBlazor
dotnet run --project src/Options/Tier2b.OptionsGeneratorBlazor
```
