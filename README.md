# async-validation-demo

Standalone demo repo that proves the async `DataAnnotations` validation API surface works across .NET technologies — Console, WinForms, WPF, Blazor, Minimal API, and MVC — without requiring multi-repo builds.

## Goal

Demonstrate that `AsyncValidationAttribute`, `Validator.*Async`, and `IAsyncValidatableObject` work correctly by running real validation scenarios across every endpoint technology. The repo ships pre-built DLLs from the `async-validation` branches of [runtime](https://github.com/ViveliDuCh/runtime/tree/async-validation) and [aspnetcore](https://github.com/ViveliDuCh/aspnetcore/tree/async-validation) so anyone can **clone and run** without building either repo.

## Folder Structure

```
async-validation-demo/
├── global.json                        ← Pins .NET 11 preview SDK
├── local-packages/                    ← Pre-built DLLs from the async-validation branches
│   ├── runtime/                       ← System.ComponentModel.Annotations.dll (+ ref/)
│   └── aspnetcore/                    ← Microsoft.Extensions.Validation.dll,
│                                        Microsoft.AspNetCore.Mvc.DataAnnotations.dll,
│                                        Microsoft.AspNetCore.Components.Forms.dll, etc.
└── src/
    ├── AsyncValidationDemo.slnx       ← Single solution for all projects
    ├── Directory.Build.props           ← Wires local-packages DLLs into all projects
    ├── SharedModels/                   ← Shared entities, validation attributes, services
    ├── Console/                        ← 2 samples
    ├── Blazor/                         ← 2 samples
    ├── MinimalApi/                     ← 3 samples
    ├── Mvc/                            ← 2 samples
    ├── WPF/                            ← 2 samples
    ├── WinForms/                       ← 4 samples
    ├── EfCore/                         ← 2 samples
    ├── OpenApi/                        ← 2 samples
    └── Options/                        ← 4 projects (1 shared lib + 3 samples)
```

- **`local-packages/`** — Pre-built DLLs from the prototype branches. `Directory.Build.props` references these directly via `<Reference HintPath="...">` so projects get the async validation APIs without NuGet or building the runtime/aspnetcore repos.
- **`src/SharedModels/`** — Shared class library with entities (`User`, `Event`, `Order`, `Profile`, `UserRegistration`, `MoneyTransfer`), async validation attributes (`IsValidName`, `AsyncOnlyEmailDomain`, `AsyncDateRangeValid`, `UniqueEmail`, `UniqueUsername`), and services (`UserService`, `SimpleServiceProvider`).
- **`src/`** — 23 runnable sample projects across 9 technologies, all referencing `SharedModels`. See [`src/README.md`](src/README.md) for per-sample documentation.

## Prototype Branches

| Repo | Branch | What it provides |
|------|--------|------------------|
| [ViveliDuCh/runtime](https://github.com/ViveliDuCh/runtime/tree/async-validation) | `async-validation` | Core async validation APIs: `AsyncValidationAttribute`, `Validator.*Async`, `IAsyncValidatableObject` → `local-packages/runtime/` |
| [ViveliDuCh/aspnetcore](https://github.com/ViveliDuCh/aspnetcore/tree/async-validation) | `async-validation` | Endpoint integration: `Microsoft.Extensions.Validation`, `Mvc.DataAnnotations`, `Components.Forms` → `local-packages/aspnetcore/` |

## Prerequisites

- **.NET 11 Preview SDK** — the exact version pinned in `global.json` (currently `11.0.100-preview.4.26210.111`). Install via:

```powershell
# Download and install the exact SDK version
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Version "11.0.100-preview.4.26210.111" -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
```

## Building and Running

The .NET 11 preview SDK is typically not installed system-wide, so every
shell session must put the local install first on PATH:

```powershell
# Run this ONCE per shell session before any build/run commands
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
$env:DOTNET_MULTILEVEL_LOOKUP = "0"
cd src

# Verify — should print the version from global.json
dotnet --version
```

### Build the entire solution

```powershell
dotnet build AsyncValidationDemo.slnx
```

### Build and run a single project

```powershell
# Console sample (runs and exits)
dotnet run --project Console\BasicAsyncSample\BasicAsyncSample.csproj

# Console advanced sample (DI, error handling, cancellation)
dotnet run --project Console\AsyncValidationConsoleDemo\AsyncValidationConsoleDemo.csproj

# Web sample (starts Kestrel — test via browser or curl)
dotnet run --project Mvc\AsyncBasicSample\AsyncBasicSample.csproj

# Desktop sample (launches GUI window)
dotnet run --project WPF\AsyncManualSample\AsyncManualSample.csproj
```

## Updating local-packages

When the `async-validation` branches are updated, rebuild the DLLs and copy them into `local-packages/`:

### Runtime DLL

```powershell
# Clone / pull the runtime fork
cd <your-runtime-repo>
git checkout async-validation
git pull

# Build the System.ComponentModel.Annotations library
.\build.cmd -subset libs.System.ComponentModel.Annotations -c Release

# Copy the implementation and ref DLLs
$artifacts = "artifacts\bin\System.ComponentModel.Annotations\Release\net11.0"
copy "$artifacts\System.ComponentModel.Annotations.dll" "<async-validation-demo>\local-packages\runtime\"
copy "$artifacts\ref\System.ComponentModel.Annotations.dll" "<async-validation-demo>\local-packages\runtime\ref\"
```

### ASP.NET Core DLLs

```powershell
# Clone / pull the aspnetcore fork
cd <your-aspnetcore-repo>
git checkout async-validation
git pull

# Build the relevant projects
.\build.cmd -projects src\Validation\src\Microsoft.Extensions.Validation.csproj
.\build.cmd -projects src\Components\Components\src\Microsoft.AspNetCore.Components.csproj
.\build.cmd -projects src\Components\Forms\src\Microsoft.AspNetCore.Components.Forms.csproj
.\build.cmd -projects src\Mvc\Mvc.DataAnnotations\src\Microsoft.AspNetCore.Mvc.DataAnnotations.csproj

# Copy the DLLs (paths depend on build configuration — check artifacts/)
copy <built-dll-path>\Microsoft.Extensions.Validation.dll "<async-validation-demo>\local-packages\aspnetcore\"
copy <built-dll-path>\Microsoft.AspNetCore.Components.Forms.dll "<async-validation-demo>\local-packages\aspnetcore\"
copy <built-dll-path>\Microsoft.AspNetCore.Mvc.DataAnnotations.dll "<async-validation-demo>\local-packages\aspnetcore\"
# ... and any other DLLs that changed
```

### Verify after update

```powershell
cd <async-validation-demo>\src
dotnet build AsyncValidationDemo.slnx
dotnet run --project Console\BasicAsyncSample\BasicAsyncSample.csproj
```
