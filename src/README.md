# System.ComponentModel.Annotations Async Validation Samples

Demonstrates the async validation APIs (`AsyncValidationAttribute`,
`IAsyncValidatableObject`, `Validator.TryValidateObjectAsync`, etc.) across
Console, WinForms, WPF, Blazor, Minimal API, MVC, EF Core, OpenAPI, and
Options applications. All form/endpoint samples share a common set of entity,
validation, and service classes via the `SharedModels` project.

The samples are organized around the **4 API proposal scenarios** from
[dotnet/runtime#128096](https://github.com/dotnet/runtime/issues/128096):

| Scenario | Entity | Interface | Key Async Feature |
|----------|--------|-----------|-------------------|
| **1** | `User` | None | Mixed sync `[IsValidName]` + async `[UsernameAvailableAsync]` property attrs |
| **2** | `Order` | `IValidatableObject` | Sync cross-property `Validate()` + async `[AsyncProductExists]` + entity attrs |
| **3** | `MoneyTransfer` | `IAsyncValidatableObject` | Async cross-property `ValidateAsync()` (same-account + balance check) |
| **4** | `Event` | `IValidatableObject` | Async entity attr `[AsyncDateRangeValid]` (calendar service → `maxDateAllowed`) |

The Options samples use a standalone `Options.Shared` library to demonstrate
`IOptions<T>` async validation at startup
([dotnet/runtime#128100](https://github.com/dotnet/runtime/issues/128100)).

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
│   ├── AsyncValidationDemo/             ← DI, two-phase, error handling, cancellation across 3 pages
│   ├── FieldLevelValidationDemo/        ← DataAnnotationsValidator field-level async (5 pages)
│   └── MevValidationDemo/              ← MEV source-gen vs fallback path comparison (5 pages)
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

Shared class library containing the entity model set plus the validation
attributes and helper services used by every non-Options sample. `Task.Delay`
is used throughout to simulate async I/O, while `UserService` demonstrates
DI-backed validation through `ValidationContext.GetService()`.

The entities map to the four API proposal scenarios from
[dotnet/runtime#128096](https://github.com/dotnet/runtime/issues/128096):

| Entity | Interface shape | API Proposal Scenario | What it demonstrates |
|--------|-----------------|----------------------|----------------------|
| `User` | No interface | Scenario 1 | Mixed sync + async property attrs: `[IsValidName]` (sync), `[UsernameAvailableAsync]` (async DB check) |
| `UserRegistration` | No interface | (Extended demo) | DI-backed async property attrs (`[UniqueUsername]`, `[UniqueEmail]`), sync entity attr (`[PasswordPolicy]`), async entity attr (`[AsyncRegistrationScreen]`) |
| `Event` | `IValidatableObject` | Scenarios 1 + 4 | Async entity attr `[AsyncDateRangeValid]` gets `maxDateAllowed` from calendar service, plus sync `Validate()` inline logic |
| `Order` | `IValidatableObject` | Scenario 2 | Sync interface validation with sync cross-property `Validate()`, plus async property attr `[AsyncProductExists]` and async/sync entity attrs |
| `MoneyTransfer` | `IAsyncValidatableObject` | Scenario 3 | Async cross-property validation: same-account check + async balance check via `ValidateAsync()` |

### Validation Attribute Inventory

| Attribute | Target | Kind | Notes |
|-----------|--------|------|-------|
| `IsValidName` | `User.Name` | Sync property | Simulates sync I/O via `Thread.Sleep` (API proposal Scenario 1) |
| `UsernameAvailableAsync` | `User.Username` | Async property | Async DB uniqueness check (API proposal Scenario 1, per Jeff's feedback) |
| `UniqueUsername` | `UserRegistration.Username` | Async property | DI-backed uniqueness check via `UserService` |
| `UniqueEmail` | `UserRegistration.Email` | Async property | DI-backed uniqueness check via `UserService` |
| `PasswordPolicy` | `UserRegistration` | Sync entity | Rejects passwords that contain the username |
| `AsyncRegistrationScreen` | `UserRegistration` | Async entity | Simulates async blocklist / fraud screening |
| `AsyncDateRangeValid` | `Event` | Async entity | Calls calendar service → gets `maxDateAllowed`, validates start < end and end ≤ max (API proposal Scenario 1) |
| `AsyncDateRangeValidWithSyncFallback` | (Scenario 4 demo) | Async entity + sync fallback | Same date range logic but overrides both `IsValidAsync` and `IsValid` for backward compat |
| `AsyncProductExists` | `Order.ProductName` | Async property | Simulates async product catalog lookup |
| `MaxOrderValue` | `Order` | Sync entity | Hard cap on order total |
| `AsyncInventoryCheck` | `Order` | Async entity | Simulates external inventory verification |
| `ReservedTitleCheck` | (Legacy demo) | Async property + sync fallback | Async under `TryValidateObjectAsync`, sync-over-async under `TryValidateObject` |
| `DateRange` | (Legacy demo) | Sync entity | Ensures `StartDate < EndDate` |
| `AsyncScheduleCheck` | (Legacy demo) | Async entity | Simulates external calendar conflict checks |

---

## Console Samples

### BasicAsyncSample

Demonstrates the 4 API proposal scenarios:

| # | Scenario | Entity | Key Feature |
|---|----------|--------|-------------|
| 1 | No interface, mixed attrs | `User` with `[IsValidName]` (sync) + `[UsernameAvailableAsync]` (async) |
| 2 | `IValidatableObject` | `Order` with sync `Validate()` + async `[AsyncProductExists]` |
| 3 | `IAsyncValidatableObject` | `MoneyTransfer` with async `ValidateAsync()` |
| 4 | Async entity + sync fallback | `Event` with `[AsyncDateRangeValid]` + timing comparison |

Includes two-phase optimization demo (sync failure skips async) and
async-parallel vs sync-sequential timing comparison for Scenario 4.

### AsyncValidationConsoleDemo

Advanced sample demonstrating the 4 scenarios with infrastructure failure
handling and `CancellationToken` propagation.

---

## WinForms Samples

### AsyncBasicSample
Programmatic UI (no designer). Four tabs matching the 4 API proposal scenarios
(`User`, `Order`, `MoneyTransfer`, `Event`) with `ErrorProvider` wired to
`Validator.TryValidateObjectAsync`.

### AsyncValidationDemo
DI-backed validation across the 4 scenarios with error handling and
cancellation-aware validation.

### AsyncDesignerBasicSample / AsyncDesignerValidationDemo
Same 4 scenarios but with designer-generated `InitializeComponent` controls.

---

## WPF Samples

### AsyncManualSample
Manual `INotifyDataErrorInfo` bridge using `ValidatableViewModelBase` (~50 LOC).
Calls `Validator.TryValidatePropertyAsync` per-property and
`Validator.TryValidateObjectAsync` on "Validate All". Four panels matching the
4 API proposal scenarios (`User`, `Order`, `MoneyTransfer`, `Event`).

### AsyncToolkitSample
Uses `CommunityToolkit.Mvvm` `ObservableValidator` for zero-boilerplate
`INotifyDataErrorInfo`. Full-object async validation via
`Validator.TryValidateObjectAsync` on button click, four panels matching the
4 API proposal scenarios.

---

## Blazor Samples

### AsyncBasicSample (Form-Level)
Blazor Server app with four validation pages matching the 4 API proposal
scenarios. Uses the **form-level** manual pattern: `EditContext` +
`ValidationMessageStore` + manual `Validator.TryValidateObjectAsync()` on
submit.

| # | Scenario | Page |
|---|----------|------|
| 1 | `User` — sync + async property attrs | `Scenario1User` |
| 2 | `Order` — `IValidatableObject` + async attrs | `Scenario2Order` |
| 3 | `MoneyTransfer` — `IAsyncValidatableObject` | `Scenario3Transfer` |
| 4 | `Event` — `[AsyncDateRangeValid]` entity attr | `Scenario4Event` |

### AsyncValidationDemo (Form-Level)
DI-backed Blazor Server app with the 4 scenarios plus infrastructure error
handling and `CancellationToken` propagation.

### FieldLevelValidationDemo (Field-Level)
Blazor Server app demonstrating **field-level async validation** via
`<DataAnnotationsValidator />` + `EditContext.ValidateAsync()`. Four scenario
pages + `PendingFaultedDemo` + `ComparisonDemo`.

| # | Scenario | Page |
|---|----------|------|
| 1 | `User` with `IsValidationPending` on Username | `Scenario1User` |
| 2 | `Order` with `IsValidationPending` on ProductName | `Scenario2Order` |
| 3 | `MoneyTransfer` — `IAsyncValidatableObject` on submit | `Scenario3Transfer` |
| 4 | `Event` — `[AsyncDateRangeValid]` on submit | `Scenario4Event` |
| — | `IsValidationPending` / `IsValidationFaulted` demo | `PendingFaultedDemo` |
| — | Side-by-side form-level vs field-level | `ComparisonDemo` |

### MevValidationDemo (MEV Source-Gen vs Fallback)
Tests the MEV (`Microsoft.Extensions.Validation`) source-gen code path in
`DataAnnotationsValidator` vs the runtime fallback. `[ValidatableType]` models
use `TryValidateTypeInfoAsync`; SharedModels entities use
`ValidateWithDefaultValidatorAsync`.

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
MVC app with four scenario controllers (`Scenario1`–`Scenario4`) matching the
API proposal. The built-in `DataAnnotationsModelValidator` implements
`IAsyncModelValidator`, so `ValidationVisitor.ValidateNodeAsync()` calls
`ValidateAsync()` during model binding — async attributes run natively.

### AsyncValidationDemo
DI-backed MVC app with the 4 scenario controllers plus `ErrorHandlingController`
for infrastructure failure handling via `UseExceptionHandler`.

---

## EF Core Samples

Proves that EF Core's convention system can detect `AsyncValidationAttribute`
subclasses via reflection and apply schema-relevant metadata — two projects,
one per registration mechanism. Now scans 4 entities (`User`, `Order`,
`MoneyTransfer`, `Event`).

### PropertyAttributeConventionDemo (Path A)
Self-contained sample using `PropertyAttributeConventionBase<UniqueUsernameAttribute>`
— typed to a specific attribute.

### ModelFinalizingConventionDemo (Path B)
References SharedModels and uses `IModelFinalizingConvention` to scan all
4 entities for `AsyncValidationAttribute` subclasses.

---

## OpenAPI Samples

Proves that async validation attributes can produce OpenAPI-compatible JSON
Schema metadata. Now includes `User` with `[UsernameAvailableAsync]`.

### OpenApiSimulation.PreAttribute (Approach A)
Reflection-based schema extraction for `User`, `Order`, `MoneyTransfer`, `Event`.

### OpenApiSimulation.SchemaDescriptor (Approach B)
Attributes implement `ISchemaDescriptor` for self-describing schema metadata.

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

#### FieldLevelValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Blazor\FieldLevelValidationDemo\FieldLevelValidationDemo.csproj
dotnet run --no-build --project Blazor\FieldLevelValidationDemo\FieldLevelValidationDemo.csproj
```

Starts Kestrel on `http://localhost:5210`. Browse to `/registration`, `/order`,
`/event` for field-level async validation with per-field spinners.
`/pending-faulted` shows `IsValidationPending` / `IsValidationFaulted` state
tracking. `/comparison` shows side-by-side form-level vs field-level code.

#### MevValidationDemo

```powershell
# Working directory: C:\REPOS\async-validation-demo\src
dotnet build Blazor\MevValidationDemo\MevValidationDemo.csproj
dotnet run --no-build --project Blazor\MevValidationDemo\MevValidationDemo.csproj
```

Starts Kestrel on `http://localhost:5214`. Browse to `/mev-registration` and
`/mev-order` for MEV source-gen path validation. `/fallback-registration` and
`/fallback-order` use SharedModels entities via the runtime Validator fallback.
`/comparison` shows both paths side-by-side.

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
    ├── Microsoft.AspNetCore.Components.Forms.dll    ← EditContext.ValidateAsync(), AddValidationTask, IsValidationPending/Faulted, field-level async
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
| `Microsoft.AspNetCore.Components.Forms.dll` | `dotnet/aspnetcore` (`async-validation` branch) | `EditContext.ValidateAsync()`, `OnAsyncValidationRequested` event, `AddValidationTask()`, `IsValidationPending()`, `IsValidationFaulted()`, field-level `TryValidatePropertyAsync` via `OnFieldChanged` |
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

### Core Runtime (`System.ComponentModel.Annotations`)

- `AsyncValidationAttribute` — Abstract base for async validation attributes
- `IAsyncValidatableObject` — Interface for object-level async validation
- `Validator.TryValidateObjectAsync` — Async version of `TryValidateObject`
- `Validator.TryValidatePropertyAsync` — Async version of `TryValidateProperty`
- `Validator.TryValidateValueAsync` — Async version of `TryValidateValue`
- `Validator.ValidateObjectAsync` — Async version of `ValidateObject`
- `Validator.ValidatePropertyAsync` — Async version of `ValidateProperty`
- `Validator.ValidateValueAsync` — Async version of `ValidateValue`

### Blazor EditContext (`Microsoft.AspNetCore.Components.Forms`)

- `EditContext.ValidateAsync()` — Async form-level validation; cancels all pending field tasks first
- `EditContext.AddValidationTask(field, task, cts)` — Tracks per-field async validation with auto-cancellation on re-type
- `EditContext.IsValidationPending(field)` — Returns `true` while a field's async validation is in flight
- `EditContext.IsValidationPending()` — Returns `true` if any field has a pending async task
- `EditContext.IsValidationFaulted(field)` — Returns `true` if a field's async validation threw an exception
- `EditContext.IsValidationFaulted()` — Returns `true` if any field has a faulted async task
- `EditContext.OnAsyncValidationRequested` — Async delegate replacing `OnValidationRequested` for form submit
