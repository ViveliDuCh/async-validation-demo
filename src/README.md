# System.ComponentModel.Annotations Async Validation Samples

Demonstrates the async validation APIs (`AsyncValidationAttribute`,
`IAsyncValidatableObject`, `Validator.TryValidateObjectAsync`, etc.) across
Console, WinForms, WPF, Blazor, Minimal API, MVC, EF Core, OpenAPI, and
Options applications. All form/endpoint samples share a common set of entity,
validation, and service classes via the `SharedModels` project.

The shared model set is intentionally organized around three validation cases:
an attribute-only entity with no interface (`UserRegistration`), a sync
`IValidatableObject` entity (`Event`), and an async
`IAsyncValidatableObject` entity (`Order`). `Event` also includes the
sync-over-async fallback pattern (Case 4) via `[ReservedTitleCheck]`. The
Options samples use a standalone `Options.Shared` library to demonstrate
`IOptions<T>` async validation at startup.

## Folder Structure

```
src/
├── AsyncValidationDemo.slnx            ← Single solution for all projects
├── Directory.Build.props                ← Wires local-packages DLLs into all projects
├── SharedModels/                        ← Class library shared by ALL samples
│   ├── EntityClasses/                   ← UserRegistration, Event, Order
│   ├── ValidationClasses/               ← UniqueUsername, UniqueEmail, PasswordPolicy,
│   │                                      AsyncRegistrationScreen, ReservedTitleCheck,
│   │                                      DateRange, AsyncScheduleCheck,
│   │                                      AsyncProductExists, MaxOrderValue,
│   │                                      AsyncInventoryCheck
│   └── ServiceClasses/                  ← UserService, SimpleServiceProvider
│
├── Console/
│   ├── BasicAsyncSample/                ← 3-case walkthrough + Case 4 timing comparison
│   └── AsyncValidationConsoleDemo/      ← DI, two-phase, cancellation, infrastructure handling
│
├── WinForms/
│   ├── AsyncBasicSample/                ← Programmatic controls + async ErrorProvider bridge (3 tabs)
│   ├── AsyncValidationDemo/             ← DI-backed async validation across 3 entities
│   ├── AsyncDesignerBasicSample/        ← Designer-generated controls + async validation (3 tabs)
│   └── AsyncDesignerValidationDemo/     ← Designer + DI + async validation across 3 entities
│
├── WPF/
│   ├── AsyncManualSample/               ← Manual INotifyDataErrorInfo async bridge (~50 LOC base, 3 panels)
│   └── AsyncToolkitSample/              ← CommunityToolkit.Mvvm ObservableValidator + async (3 panels)
│
├── Blazor/
│   ├── AsyncBasicSample/                ← EditContext + ValidationMessageStore async bridge (3 pages)
│   └── AsyncValidationDemo/             ← DI, two-phase, error handling, cancellation across 3 pages
│
├── MinimalApi/
│   ├── AsyncBasicSample/                ← Manual TryValidateObjectAsync for 3 entity endpoint groups
│   ├── AsyncValidationDemo/             ← DI + real CancellationToken propagation for 3 entities
│   └── AutoValidationSample/            ← Hybrid: AddValidation() + manual async for SharedModels
│
├── Mvc/
│   ├── AsyncBasicSample/                ← Cleared ModelValidatorProviders + 3 async controllers
│   └── AsyncValidationDemo/             ← DI, async controllers, infrastructure handling across 3 entities
│
├── EfCore/
│   ├── PropertyAttributeConventionDemo/ ← Path A: PropertyAttributeConventionBase<T> (self-contained)
│   └── ModelFinalizingConventionDemo/   ← Path B: IModelFinalizingConvention (3 entities / 7 async attrs)
│
├── OpenApi/
│   ├── OpenApiSimulation.PreAttribute/  ← Zero-modification: reflection-based schema extraction
│   └── OpenApiSimulation.SchemaDescriptor/ ← ISchemaDescriptor interface for self-describing attrs
│
└── Options/
    ├── Options.Shared/                  ← CloudInfoOptions POCO + AsyncStorageExistsAttribute
    ├── AsyncLambdaConsole/              ← Inline async lambda validation at startup
    ├── Tier2.OptionsBlazor/             ← Bypass approach (reflection-based)
    └── Tier2b.OptionsGeneratorBlazor/   ← Source generator approach (AOT-friendly)
```

---

## SharedModels

Shared class library containing the refactored three-entity model set plus the
validation attributes and helper services used by every non-Options sample.
`Task.Delay` is used throughout to simulate async I/O, while `UserService`
demonstrates DI-backed validation through `ValidationContext.GetService()`.

The entities intentionally map to the three core validation shapes, with one
extra fallback pattern layered onto `Event`:

| Entity | Interface shape | What it demonstrates |
|--------|------------------|----------------------|
| `UserRegistration` | No interface | Pure attribute-driven validation: DI-backed async property attrs (`[UniqueUsername]`, `[UniqueEmail]`), sync entity attr (`[PasswordPolicy]`), async entity attr (`[AsyncRegistrationScreen]`) |
| `Event` | `IValidatableObject` | Sync interface validation plus sync/async attributes: `[ReservedTitleCheck]`, `[DateRange]`, `[AsyncScheduleCheck]`, and inline `Validate()` logic |
| `Order` | `IAsyncValidatableObject` | Async interface validation plus sync/async attributes: `[AsyncProductExists]`, `[MaxOrderValue]`, `[AsyncInventoryCheck]`, and inline `ValidateAsync()` logic |

### Validation Attribute Inventory

| Attribute | Target | Kind | Notes |
|-----------|--------|------|-------|
| `UniqueUsername` | `UserRegistration.Username` | Async property | DI-backed uniqueness check via `UserService` |
| `UniqueEmail` | `UserRegistration.Email` | Async property | DI-backed uniqueness check via `UserService` |
| `PasswordPolicy` | `UserRegistration` | Sync entity | Rejects passwords that contain the username |
| `AsyncRegistrationScreen` | `UserRegistration` | Async entity | Simulates async blocklist / fraud screening |
| `ReservedTitleCheck` | `Event.Title` | Async property + sync fallback | Case 4: async under `TryValidateObjectAsync`, blocking sync-over-async under `TryValidateObject` |
| `DateRange` | `Event` | Sync entity | Ensures `StartDate < EndDate` |
| `AsyncScheduleCheck` | `Event` | Async entity | Simulates external calendar conflict checks |
| `AsyncProductExists` | `Order.ProductName` | Async property | Simulates async product catalog lookup |
| `MaxOrderValue` | `Order` | Sync entity | Hard cap on order total |
| `AsyncInventoryCheck` | `Order` | Async entity | Simulates external inventory verification |

Case 4 is represented by `[ReservedTitleCheck]`: async callers use
`Validator.TryValidateObjectAsync` and stay non-blocking, while sync callers
using `Validator.TryValidateObject` force the attribute through a deliberate
sync-over-async fallback to illustrate the tradeoff.

---

## Console Samples

### BasicAsyncSample

Minimal sample covering the refactored SharedModels design:

| # | Pattern | Mechanism |
|---|---------|-----------|
| 1 | Case 1 — attribute-only entity | `UserRegistration` with sync + async property/entity attributes |
| 2 | Case 2 — sync interface entity | `Event` with `IValidatableObject`, `[DateRange]`, `[AsyncScheduleCheck]`, and `[ReservedTitleCheck]` |
| 3 | Case 3 — async interface entity | `Order` with `IAsyncValidatableObject`, `[AsyncProductExists]`, `[MaxOrderValue]`, and `[AsyncInventoryCheck]` |
| 4 | Case 4 — sync-over-async fallback | `ReservedTitleCheck` timing comparison: async-parallel vs sync-blocking |

Includes an async-parallel vs sync-sequential timing comparison for
`Event.Title` to show the Case 4 fallback behavior.

### AsyncValidationConsoleDemo

Advanced sample that reuses the same three entities with DI integration,
two-phase validation, `IValidatableObject`, `IAsyncValidatableObject`,
infrastructure failure handling, and cancellation token propagation.

---

## WinForms Samples

### AsyncBasicSample
Programmatic UI (no designer). Three tabs (`UserRegistration`, `Event`,
`Order`) with `ErrorProvider` wired to `Validator.TryValidateObjectAsync`.

### AsyncValidationDemo
DI-backed validation using `UserRegistration`, `Event`, and `Order`.
Demonstrates duplicate detection, two-phase validation, Case 4
sync-over-async fallback, `IValidatableObject`, `IAsyncValidatableObject`,
infrastructure error handling, and cancellation-aware validation.

### AsyncDesignerBasicSample / AsyncDesignerValidationDemo
Same scenarios as above but with designer-generated `InitializeComponent`
controls. The basic and DI-backed designer samples now mirror the same
three-tab entity layout as the code-first WinForms samples.

---

## WPF Samples

### AsyncManualSample
Manual `INotifyDataErrorInfo` bridge using `ValidatableViewModelBase` (~50 LOC).
Calls `Validator.TryValidatePropertyAsync` per-property and
`Validator.TryValidateObjectAsync` on "Validate All". The window now presents
three SharedModels panels (`UserRegistration`, `Event`, `Order`) and remains
responsive throughout validation.

### AsyncToolkitSample
Uses `CommunityToolkit.Mvvm` `ObservableValidator` for zero-boilerplate
`INotifyDataErrorInfo`. Full-object async validation via
`Validator.TryValidateObjectAsync` on button click, again using the same three
entity panels.

---

## Blazor Samples

### AsyncBasicSample
Blazor Server app with three validation pages (`UserRegistration`, `Event`,
`Order`). Since Blazor's built-in `DataAnnotationsValidator` only supports sync
validation, this sample uses `EditContext` + `ValidationMessageStore` to bridge
async validation into the Blazor form system. `<ValidationMessage>` and
`<ValidationSummary>` tag helpers still display errors correctly.

All pages pass a `CancellationToken` to `TryValidateObjectAsync` and subscribe
to `EditContext.OnFieldChanged` — editing any field while validation is in
flight automatically cancels the running async check via
`CancellationTokenSource`. The submit button stays enabled so re-clicking
cancels and restarts validation. Each page implements `IDisposable` to clean up
the event subscription and `CancellationTokenSource`.

| # | Pattern | Page |
|---|---------|------|
| 1 | Case 1 — attribute-only validation | `RegistrationValidation` |
| 2 | Case 2 — `IValidatableObject` + Case 4 fallback | `EventValidation` |
| 3 | Case 3 — `IAsyncValidatableObject` | `OrderValidation` |

### AsyncValidationDemo
DI-backed Blazor Server app demonstrating `UserService` resolution via
`IServiceProvider` in `ValidationContext`, two-phase validation (sync attr
fails → async attr skipped), `IValidatableObject`, `IAsyncValidatableObject`,
infrastructure error handling, and `CancellationToken` propagation (not
possible with sync validation). All three pages support cancel-on-field-change
via `EditContext.OnFieldChanged` — the same pattern as AsyncBasicSample.

---

## Minimal API Samples

### AsyncBasicSample
Minimal API with three SharedModels endpoint groups, each validating request
bodies using `Validator.TryValidateObjectAsync`. Mirrors the console
`BasicAsyncSample` cases as HTTP endpoints for `UserRegistration`, `Event`, and
`Order`.

### AsyncValidationDemo
DI-backed Minimal API with `UserService` registration, duplicate detection,
two-phase validation, `IValidatableObject`, `IAsyncValidatableObject`,
infrastructure error handling, and real `CancellationToken` propagation via
`HttpContext.RequestAborted`.

### AutoValidationSample (Hybrid)
Demonstrates that .NET 10's `AddValidation()` automatic validation is **not**
async validation. Local models (Customer, Address, DemoOrder, ContactFormModel)
use `AddValidation()` with standard sync attributes. SharedModels endpoints
use `.DisableValidation()` and manual `Validator.TryValidateObjectAsync` for
the refactored `UserRegistration`, `Event`, and `Order` models. This hybrid
approach explicitly illustrates when automatic validation suffices and when
async validation is needed.

---

## MVC Samples

### AsyncBasicSample
MVC app with three entity controllers (`Registration`, `Event`, `Order`).
Built-in `DataAnnotationsModelValidatorProvider` is cleared via
`ModelValidatorProviders.Clear()` to prevent sync blocking during model
binding. Controller POST actions are `async Task<IActionResult>` and manually
call `Validator.TryValidateObjectAsync`, adding errors to `ModelState` so
existing Razor tag helpers display them without view changes.

### AsyncValidationDemo
DI-backed MVC app with `UserService`, duplicate detection, `Event` and `Order`
validation, and infrastructure failure handling. Uses
`HttpContext.RequestServices` as the `IServiceProvider` in `ValidationContext`.
The `ErrorHandlingController` wraps async validation in try/catch and uses
`UseExceptionHandler` for unhandled errors.

---

## EF Core Samples

Proves that EF Core's convention system can detect `AsyncValidationAttribute`
subclasses via reflection and apply schema-relevant metadata — two projects,
one per registration mechanism.

### PropertyAttributeConventionDemo (Path A)
Self-contained sample using `PropertyAttributeConventionBase<UniqueUsernameAttribute>`
— typed to a specific attribute. Mirrors how EF Core's built-in conventions
work (e.g., `[Required]` → NOT NULL). Demonstrates: convention detection,
UNIQUE INDEX creation, annotation storage, generated SQL.

### ModelFinalizingConventionDemo (Path B)
References SharedModels and uses `IModelFinalizingConvention` to scan ALL
3 entities for ANY `AsyncValidationAttribute` subclass. Detects 7 async
attributes (4 property-level, 3 class-level), creates 2 UNIQUE indexes, stores
7 annotations, and ignores interface methods such as `Event.Validate()` and
`Order.ValidateAsync()` because EF Core conventions only react to attributes.

---

## OpenAPI Samples

Proves that async validation attributes can produce OpenAPI-compatible JSON
Schema metadata via a simulated `IOpenApiSchemaTransformer`. Two console
projects, one per schema-extraction approach.

### OpenApiSimulation.PreAttribute (Approach A)
**Zero attribute modification** — discovers `AsyncValidationAttribute`
subclasses via pure reflection and extracts metadata by convention to produce
`x-async-validation` and `x-requires-server-check` schema extensions for the
refactored `UserRegistration`, `Event`, and `Order` schemas.

### OpenApiSimulation.SchemaDescriptor (Approach B)
Attributes implement `ISchemaDescriptor` to self-describe their schema
contributions (description, format, pattern, extensions). Produces richer
metadata than Approach A while still mapping back to the same three
SharedModels entity shapes.

---

## Options Samples

Demonstrates async validation of `IOptions<T>` configuration at application
startup.

### Options.Shared
Standalone class library containing the shared POCO (`CloudInfoOptions`) and
async attribute (`AsyncStorageExistsAttribute`).

### AsyncLambdaConsole
Console app demonstrating inline async lambda validation with
`.ValidateAsync<TDep>()` — validates options at startup without implementing
`IAsyncValidateOptions<T>` as a separate class.

### Tier2.OptionsBlazor (Bypass Approach)
Uses `.ValidateDataAnnotationsAsync().ValidateOnStartAsync()` extension methods.

### Tier2b.OptionsGeneratorBlazor (Source Generator)
Uses `[OptionsValidator]` source generator to emit both `Validate()` and
`ValidateAsync()` at compile time — no reflection at runtime.

---

## Building and Running

> **Before building**, ensure the DLLs in `local-packages/` are up to date with
> the latest changes from both `async-validation` branches. If any changes have
> been made to either repo, follow the steps in
> [How to Rebuild Local Packages](#how-to-rebuild-local-packages) to regenerate
> and copy the DLLs, then return here to build and run.

All commands below are run from `C:\REPOS\async-validation-demo\src`. The
.NET 11 preview SDK pinned in `global.json` is typically not installed
system-wide, so every shell session must set these variables first:

```powershell
# Run this ONCE per shell session before any build/run commands
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
$env:DOTNET_MULTILEVEL_LOOKUP = "0"
cd C:\REPOS\async-validation-demo\src
```

This ensures `dotnet` resolves to the locally-installed .NET 11 SDK instead of
the system-wide .NET 10. Verify with `dotnet --version` — it should print the
version from `global.json`.

### Console

#### BasicAsyncSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Console\BasicAsyncSample\BasicAsyncSample.csproj
dotnet run --no-build --project Console\BasicAsyncSample\BasicAsyncSample.csproj
```

Expected output: a walkthrough of the three SharedModels cases plus the Case 4
timing comparison — attribute-only `UserRegistration`, `Event` sync-vs-async
`ReservedTitleCheck` timing with entity-level date/schedule failures, and
`Order` async business-rule failures.

#### AsyncValidationConsoleDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Console\AsyncValidationConsoleDemo\AsyncValidationConsoleDemo.csproj
dotnet run --no-build --project Console\AsyncValidationConsoleDemo\AsyncValidationConsoleDemo.csproj
```

Expected output: scenarios covering DI-backed duplicate detection
(`UserRegistration`), two-phase short-circuiting, `Event`
`IValidatableObject` validation, `Order` `IAsyncValidatableObject` validation,
infrastructure failure (`InvalidOperationException` caught), and cancellation
(`OperationCanceledException` caught).

### WinForms

#### AsyncBasicSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build WinForms\AsyncBasicSample\AsyncBasicSample.csproj
dotnet run --no-build --project WinForms\AsyncBasicSample\AsyncBasicSample.csproj
```

Launches a tabbed WinForms window (`UserRegistration`, `Event`, `Order`).
Click "Validate All (Async)" on each tab to trigger async validation.

#### AsyncValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build WinForms\AsyncValidationDemo\AsyncValidationDemo.csproj
dotnet run --no-build --project WinForms\AsyncValidationDemo\AsyncValidationDemo.csproj
```

Launches a DI-backed tabbed window for the same three entities,
demonstrating duplicate checks, two-phase validation, interface-based entity
validation, infrastructure failure handling, and cancellation-aware flows.

#### AsyncDesignerBasicSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build WinForms\AsyncDesignerBasicSample\AsyncDesignerBasicSample.csproj
dotnet run --no-build --project WinForms\AsyncDesignerBasicSample\AsyncDesignerBasicSample.csproj
```

Designer-based equivalent of `AsyncBasicSample`, using the same three-tab
layout.

#### AsyncDesignerValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build WinForms\AsyncDesignerValidationDemo\AsyncDesignerValidationDemo.csproj
dotnet run --no-build --project WinForms\AsyncDesignerValidationDemo\AsyncDesignerValidationDemo.csproj
```

Designer-based equivalent of `AsyncValidationDemo`, again using the same three
entity workflows.

### WPF

#### AsyncManualSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build WPF\AsyncManualSample\AsyncManualSample.csproj
dotnet run --no-build --project WPF\AsyncManualSample\AsyncManualSample.csproj
```

Launches a WPF window with three entity panels. Validation errors appear as red
borders + tooltips via `INotifyDataErrorInfo`.

#### AsyncToolkitSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build WPF\AsyncToolkitSample\AsyncToolkitSample.csproj
dotnet run --no-build --project WPF\AsyncToolkitSample\AsyncToolkitSample.csproj
```

Uses CommunityToolkit.Mvvm `ObservableValidator` with async validation across
the same three entity panels.

### Blazor

#### AsyncBasicSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Blazor\AsyncBasicSample\AsyncBasicSample.csproj
dotnet run --no-build --project Blazor\AsyncBasicSample\AsyncBasicSample.csproj
```

Starts Kestrel on `http://localhost:5200`. Browse to `/registration`,
`/event`, `/order` to test async validation in Blazor forms.

#### AsyncValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Blazor\AsyncValidationDemo\AsyncValidationDemo.csproj
dotnet run --no-build --project Blazor\AsyncValidationDemo\AsyncValidationDemo.csproj
```

Starts Kestrel on `http://localhost:5202`. Browse to `/registration`,
`/event`, `/order` to test DI-backed async validation in Blazor forms. Try
editing a field while validation is in progress to see `CancellationToken`
cancel the in-flight check.

### Minimal API

#### AsyncBasicSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build MinimalApi\AsyncBasicSample\AsyncBasicSample.csproj
dotnet run --no-build --project MinimalApi\AsyncBasicSample\AsyncBasicSample.csproj
```

Starts Kestrel on `http://localhost:5204`. The root endpoint documents the
three SharedModels endpoint groups (`UserRegistration`, `Event`, `Order`) used
to exercise the basic async-validation cases.

#### AsyncValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build MinimalApi\AsyncValidationDemo\AsyncValidationDemo.csproj
dotnet run --no-build --project MinimalApi\AsyncValidationDemo\AsyncValidationDemo.csproj
```

Starts Kestrel on `http://localhost:5206`. Endpoints are grouped around
`UserRegistration`, `Event`, and `Order` workflows, including DI-backed
validation, infrastructure failure, and cancellation scenarios.

#### AutoValidationSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build MinimalApi\AutoValidationSample\AutoValidationSample.csproj
dotnet run --no-build --project MinimalApi\AutoValidationSample\AutoValidationSample.csproj
```

Starts Kestrel on `http://localhost:5208`. Hybrid sample: local models use
`AddValidation()`, while SharedModels `UserRegistration`, `Event`, and `Order`
endpoints use manual async validation.

### MVC

#### AsyncBasicSample

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Mvc\AsyncBasicSample\AsyncBasicSample.csproj
dotnet run --no-build --project Mvc\AsyncBasicSample\AsyncBasicSample.csproj
```

Starts Kestrel on `http://localhost:5210`. Browse to `/Registration`, `/Event`,
`/Order` to test async validation in MVC controllers.

#### AsyncValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Mvc\AsyncValidationDemo\AsyncValidationDemo.csproj
dotnet run --no-build --project Mvc\AsyncValidationDemo\AsyncValidationDemo.csproj
```

Starts Kestrel on `http://localhost:5212`. Browse to `/Registration`, `/Event`,
`/Order` for DI-backed async validation.

### EfCore

#### PropertyAttributeConventionDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build EfCore\PropertyAttributeConventionDemo\PropertyAttributeConventionDemo.csproj
dotnet run --no-build --project EfCore\PropertyAttributeConventionDemo\PropertyAttributeConventionDemo.csproj
```

Console app. Expected output: convention detection of `[UniqueUsername]`,
UNIQUE INDEX creation, annotation storage, and generated SQL.

#### ModelFinalizingConventionDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build EfCore\ModelFinalizingConventionDemo\ModelFinalizingConventionDemo.csproj
dotnet run --no-build --project EfCore\ModelFinalizingConventionDemo\ModelFinalizingConventionDemo.csproj
```

Console app. Expected output: full attribute scan across all 3 SharedModels
entities, detecting 7 async attributes, storing 7 annotations, and creating 2
UNIQUE indexes.

### OpenApi

#### OpenApiSimulation.PreAttribute

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build OpenApi\OpenApiSimulation.PreAttribute\OpenApiSimulation.PreAttribute.csproj
dotnet run --no-build --project OpenApi\OpenApiSimulation.PreAttribute\OpenApiSimulation.PreAttribute.csproj
```

Console app. Expected output: JSON Schema comparisons for `UserRegistration`,
`Event`, and `Order`, showing where `x-async-validation` extensions appear.

#### OpenApiSimulation.SchemaDescriptor

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build OpenApi\OpenApiSimulation.SchemaDescriptor\OpenApiSimulation.SchemaDescriptor.csproj
dotnet run --no-build --project OpenApi\OpenApiSimulation.SchemaDescriptor\OpenApiSimulation.SchemaDescriptor.csproj
```

Console app. Expected output: comparison of original vs `ISchemaDescriptor`-
enabled schemas for the refactored SharedModels entities with richer metadata.

### Options

#### AsyncLambdaConsole

```powershell
# Working directory: C:\REPOS\async-validation-demo\src\Options\AsyncLambdaConsole
cd C:\REPOS\async-validation-demo\src\Options\AsyncLambdaConsole
dotnet build
dotnet run --no-build
```

Console app. Must be run from its project directory (requires `appsettings.json`).
Expected output: 3 scenarios — valid config, invalid endpoint caught, multiple
chained async lambdas.

#### Options.Shared (library — no run)

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Options\Options.Shared\Options.Shared.csproj
```

Class library only — referenced by the Tier2 and Tier2b Blazor samples.

#### Tier2.OptionsBlazor

```powershell
# Working directory: C:\REPOS\async-validation-demo\src\Options\Tier2.OptionsBlazor
cd C:\REPOS\async-validation-demo\src\Options\Tier2.OptionsBlazor
dotnet build
dotnet run --no-build
```

Blazor Server app. Must be run from its project directory (requires
`appsettings.json`). Browse to the displayed URL to verify startup validation
passed.

#### Tier2b.OptionsGeneratorBlazor

```powershell
# Working directory: C:\REPOS\async-validation-demo\src\Options\Tier2b.OptionsGeneratorBlazor
cd C:\REPOS\async-validation-demo\src\Options\Tier2b.OptionsGeneratorBlazor
dotnet build
dotnet run --no-build
```

Blazor Server app using `[OptionsValidator]` source generator. Must be run
from its project directory.

---

## Known Limitations

### Blazor

- **Interactive validation requires a browser.** Blazor uses SignalR circuits
  for interactive server rendering. SSR page rendering can be verified via HTTP,
  but full async form validation (submit → validate → re-render) requires a
  browser session. Automated HTTP testing only confirms that pages serve and
  contain the expected form elements.

### WPF

- **Fire-and-forget per-property validation has no exception handling.** The
  `ValidatableViewModelBase.SetAndValidateAsync()` method uses
  `_ = ValidatePropertyAsync(...)` which is fire-and-forget. If a
  property-level async validator throws, the exception goes to
  `TaskScheduler.UnobservedTaskException` rather than surfacing in the UI.
  Full-object validation via `ValidateAllAsync()` is properly wrapped in
  `try/catch` at the button handler level.

- **GUI testing is manual.** WPF samples launch successfully and respond to
  user interaction, but automated validation flow testing requires UI
  automation tooling (e.g., `Microsoft.Windows.Apps.Test`).

### WinForms

- **GUI testing is manual.** Same as WPF — WinForms samples launch and respond
  to interaction, but full validation flow testing requires manual clicks or UI
  automation.

### EfCore

- **No runtime validation.** EF Core never calls `IsValidAsync()` — conventions
  only use reflection for attribute detection and schema generation.
- **EF Core 11 API changes.** `GetIdentifyingMemberInfo()` was removed;
  `IConventionTypeBaseBuilder` no longer exposes `HasIndex` — must cast to
  `IConventionEntityType`.

### Options

- **`AsyncLambdaConsole` must run from its project directory.** It requires
  `appsettings.json` which is only found when the working directory is the
  project folder.
- **Tier2/Tier2b Blazor apps must run from their project directories.** Same
  reason — they depend on `appsettings.json` at the working directory level.
- **Hosting layer does not call `IAsyncStartupValidator` automatically.** The
  stock .NET 11 preview SDK hosting infrastructure does not yet know about
  `IAsyncStartupValidator`. Both Tier2 and Tier2b `Program.cs` files manually
  call `await app.Services.GetService<IAsyncStartupValidator>()?.ValidateAsync()`
  as a workaround.
- **Tier 2b: no two-phase short-circuit.** The source generator validates each
  property independently via `TryValidateValueAsync()`, so sync failures on one
  property do not prevent async checks on other properties.

---

## Local Packages — Build Process

The `local-packages/` directory contains locally-built DLLs from two repos
that provide the async validation APIs. These DLLs are checked into the repo
so samples can build without requiring a full runtime or aspnetcore build.

### Directory Structure

```
local-packages/
├── System.ComponentModel.Annotations.dll   ← Legacy flat copy (kept for compat)
├── runtime/
│   ├── System.ComponentModel.Annotations.dll       ← Implementation DLL
│   └── ref/
│       └── System.ComponentModel.Annotations.dll   ← Ref assembly (compiler type info)
└── aspnetcore/
    ├── Microsoft.Extensions.Validation.dll          ← Async-aware leaf calls
    ├── Microsoft.AspNetCore.Components.Forms.dll    ← EditContext.ValidateAsync()
    ├── Microsoft.AspNetCore.Mvc.DataAnnotations.dll ← IAsyncModelValidator
    ├── Microsoft.AspNetCore.Components.dll           ← Transitive dependency
    ├── Microsoft.AspNetCore.Mvc.Core.dll              ← Transitive dependency
    └── Microsoft.AspNetCore.Mvc.Abstractions.dll      ← Transitive dependency
```

### What Each DLL Provides

| DLL | Source | Async APIs Added |
|-----|--------|------------------|
| `System.ComponentModel.Annotations.dll` | `dotnet/runtime` (`async-validation` branch) | `AsyncValidationAttribute`, `IAsyncValidatableObject`, `Validator.TryValidateObjectAsync`, `TryValidatePropertyAsync`, `TryValidateValueAsync` |
| `Microsoft.Extensions.Validation.dll` | `dotnet/aspnetcore` (`async-validation` branch) | `ValidatablePropertyInfo`, `ValidatableTypeInfo` — `is AsyncValidationAttribute` branching + `IAsyncValidatableObject.ValidateAsync` support |
| `Microsoft.AspNetCore.Components.Forms.dll` | `dotnet/aspnetcore` (`async-validation` branch) | `EditContext.ValidateAsync()`, `OnAsyncValidationRequested` event |
| `Microsoft.AspNetCore.Mvc.DataAnnotations.dll` | `dotnet/aspnetcore` (`async-validation` branch) | `IAsyncModelValidator`, `DataAnnotationsModelValidator.ValidateAsync()` |

### How to Rebuild Local Packages

If you need to regenerate these DLLs (e.g., after making changes to the
runtime or aspnetcore async-validation branches):

#### Step 1: Build Runtime

```powershell
# Clone/checkout the runtime async-validation branch
cd C:\REPOS\runtime    # branch: async-validation

# Build the runtime libraries (produces impl + ref DLLs)
.\build.cmd clr+libs -rc Debug

# The DLLs we need:
#   artifacts\bin\System.ComponentModel.Annotations\Debug\net11.0\System.ComponentModel.Annotations.dll  (impl)
#   artifacts\bin\microsoft.netcore.app.ref\ref\net11.0\System.ComponentModel.Annotations.dll             (ref)
```

#### Step 2: Inject Runtime DLL into Aspnetcore SDK

The aspnetcore build resolves `System.ComponentModel.Annotations` from its
NuGet package cache. The stock version does not contain async types, so it
must be replaced with the runtime-built version before building aspnetcore.

```powershell
cd C:\REPOS\aspnetcore    # branch: async-validation

# 1. Restore to bootstrap the .NET 11 preview SDK
.\restore.cmd

# 2. Find where the SDK resolves System.ComponentModel.Annotations.dll
#    (typically C:\Nuget\microsoft.netcore.app.ref\<version>\ref\net11.0\)
$dotnet = ".\.dotnet\dotnet.exe"
& $dotnet msbuild src\Validation\src\Microsoft.Extensions.Validation.csproj `
    /t:ResolveAssemblyReferences /v:diag 2>&1 |
    Select-String "System.ComponentModel.Annotations.dll" |
    Select-Object -First 3

# 3. Replace the stock DLL with the async-enabled one
$nugetRefDir = "C:\Nuget\microsoft.netcore.app.ref\<version>\ref\net11.0"
Copy-Item "$nugetRefDir\System.ComponentModel.Annotations.dll" `
          "$nugetRefDir\System.ComponentModel.Annotations.dll.stock-backup"
Copy-Item "C:\REPOS\runtime\artifacts\bin\microsoft.netcore.app.ref\ref\net11.0\System.ComponentModel.Annotations.dll" `
          "$nugetRefDir\System.ComponentModel.Annotations.dll"
```

#### Step 3: Build Aspnetcore Projects

```powershell
$dotnet = "C:\REPOS\aspnetcore\.dotnet\dotnet.exe"

# Tier 7: Microsoft.Extensions.Validation (async-aware leaf calls)
& $dotnet build src\Validation\src\Microsoft.Extensions.Validation.csproj -c Release

# Tier 4: Blazor Forms (EditContext.ValidateAsync)
& $dotnet build src\Components\Forms\src\Microsoft.AspNetCore.Components.Forms.csproj -c Release

# Tier 3: MVC DataAnnotations (IAsyncModelValidator)
& $dotnet build src\Mvc\Mvc.DataAnnotations\src\Microsoft.AspNetCore.Mvc.DataAnnotations.csproj -c Release
```

#### Step 4: Copy DLLs to local-packages

```powershell
$localPkgs = "C:\REPOS\async-validation-demo\local-packages"
$aspBin    = "C:\REPOS\aspnetcore\artifacts\bin"
$rtBin     = "C:\REPOS\runtime\artifacts\bin"

# Runtime
Copy-Item "$rtBin\System.ComponentModel.Annotations\Debug\net11.0\System.ComponentModel.Annotations.dll" `
          "$localPkgs\runtime\System.ComponentModel.Annotations.dll"
Copy-Item "$rtBin\microsoft.netcore.app.ref\ref\net11.0\System.ComponentModel.Annotations.dll" `
          "$localPkgs\runtime\ref\System.ComponentModel.Annotations.dll"

# Aspnetcore
Copy-Item "$aspBin\Microsoft.Extensions.Validation\Release\net11.0\Microsoft.Extensions.Validation.dll" `
          "$localPkgs\aspnetcore\"
Copy-Item "$aspBin\Microsoft.AspNetCore.Components.Forms\Release\net11.0\Microsoft.AspNetCore.Components.Forms.dll" `
          "$localPkgs\aspnetcore\"
Copy-Item "$aspBin\Microsoft.AspNetCore.Mvc.DataAnnotations\Release\net11.0\Microsoft.AspNetCore.Mvc.DataAnnotations.dll" `
          "$localPkgs\aspnetcore\"
```

### How Directory.Build.props Wires It Up

The `src/Directory.Build.props` references the local DLLs so all sample
projects automatically get the async validation APIs:

```xml
<Project>
  <PropertyGroup>
    <LocalPackagesDir>$(MSBuildThisFileDirectory)..\local-packages</LocalPackagesDir>
  </PropertyGroup>

  <!-- Runtime: async validation APIs — all projects -->
  <ItemGroup>
    <Reference Include="System.ComponentModel.Annotations"
               HintPath="$(LocalPackagesDir)\runtime\System.ComponentModel.Annotations.dll"
               Private="true" />
  </ItemGroup>

  <!-- Aspnetcore: async-aware validation infrastructure — web projects only -->
  <ItemGroup Condition="'$(UsingMicrosoftNETSdkWeb)' == 'true'">
    <Reference Include="Microsoft.Extensions.Validation"
               HintPath="$(LocalPackagesDir)\aspnetcore\Microsoft.Extensions.Validation.dll"
               Private="true" />
    <Reference Include="Microsoft.AspNetCore.Components.Forms"
               HintPath="$(LocalPackagesDir)\aspnetcore\Microsoft.AspNetCore.Components.Forms.dll"
               Private="true" />
    <Reference Include="Microsoft.AspNetCore.Mvc.DataAnnotations"
               HintPath="$(LocalPackagesDir)\aspnetcore\Microsoft.AspNetCore.Mvc.DataAnnotations.dll"
               Private="true" />
  </ItemGroup>

</Project>
```

### Dependency Chain

```
dotnet/runtime                          dotnet/aspnetcore
(async-validation branch)               (async-validation branch)
┌──────────────────────────────┐       ┌─────────────────────────────────────┐
│ System.ComponentModel.       │       │ Microsoft.Extensions.Validation     │
│   Annotations.dll            │──────▶│   (async leaf calls)               │
│                              │       │                                     │
│ Types:                       │       │ Microsoft.AspNetCore.Components     │
│  • AsyncValidationAttribute  │       │   .Forms (ValidateAsync)           │
│  • IAsyncValidatableObject   │       │                                     │
│  • Validator.*Async()        │       │ Microsoft.AspNetCore.Mvc            │
│  • GetValidationResultAsync  │       │   .DataAnnotations                 │
│                              │       │    (IAsyncModelValidator)           │
└──────────────────────────────┘       └─────────────────────────────────────┘
         │                                           │
         ▼                                           ▼
┌──────────────────────────────────────────────────────────────┐
│                  async-validation-demo                        │
│                                                              │
│  local-packages/runtime/    ← Runtime DLLs                   │
│  local-packages/aspnetcore/ ← Aspnetcore DLLs                │
│  src/Directory.Build.props  ← Wires all into all projects    │
│                                                              │
│  Console, WinForms, WPF              → runtime DLLs only     │
│  Blazor, MinimalApi, MVC             → runtime + aspnetcore   │
└──────────────────────────────────────────────────────────────┘
```

---

## Async API Surface

- `AsyncValidationAttribute` — Abstract base for async validation attributes
- `IAsyncValidatableObject` — Interface for object-level async validation
- `Validator.TryValidateObjectAsync` — Async version of `TryValidateObject`
- `Validator.TryValidatePropertyAsync` — Async version of `TryValidateProperty`
- `Validator.TryValidateValueAsync` — Async version of `TryValidateValue`
- `Validator.ValidateObjectAsync` — Async version of `ValidateObject`
- `Validator.ValidatePropertyAsync` — Async version of `ValidateProperty`
- `Validator.ValidateValueAsync` — Async version of `ValidateValue`
