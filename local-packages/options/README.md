# Options Local Packages (Tier 2 / Tier 2b)

These DLLs are built from [`dotnet/runtime` PR #128788](https://github.com/dotnet/runtime/pull/128788)
(commit `572547a9f90a3ce2c6042fb9517569975bd39c59`).

## Contents

| DLL | Purpose |
|-----|---------|
| `Microsoft.Extensions.Options.dll` | Contains `IAsyncValidateOptions<T>`, `IAsyncStartupValidator`, `StartupValidator` (implements both `IStartupValidator` and `IAsyncStartupValidator`), `StartupValidatorOptions` with `_validators` and `_asyncValidators` dictionaries, `ValidateOnStart()` with TryAddTransient for both interfaces |
| `Microsoft.Extensions.Options.DataAnnotations.dll` | Contains `DataAnnotationValidateOptionsAsync<T>`, `ValidateDataAnnotationsAsync()` extension method |
| `Microsoft.Extensions.Options.SourceGeneration.dll` | Roslyn analyzer/source generator — emits `Validate()` and `ValidateAsync()` for `[OptionsValidator]` classes that implement `IAsyncValidateOptions<T>` |

## How to Rebuild

From the `dotnet/runtime` repo at PR #128788 head:

```powershell
# Fetch and checkout PR #128788
git fetch upstream pull/128788/head:pr-128788
git checkout pr-128788

# Build (using the repo's local SDK)
dotnet build src\libraries\Microsoft.Extensions.Options\src\Microsoft.Extensions.Options.csproj -c Release
dotnet build src\libraries\Microsoft.Extensions.Options.DataAnnotations\src\Microsoft.Extensions.Options.DataAnnotations.csproj -c Release
```

Then copy from `artifacts\bin\`:
- `Microsoft.Extensions.Options\Release\net11.0\Microsoft.Extensions.Options.dll`
- `Microsoft.Extensions.Options.DataAnnotations\Release\net11.0\Microsoft.Extensions.Options.DataAnnotations.dll`
- `Microsoft.Extensions.Options.SourceGeneration\Release\netstandard2.0\Microsoft.Extensions.Options.SourceGeneration.dll`

> **Note:** The source generator DLL must be the `netstandard2.0` build (Roslyn
> analyzers are required to target netstandard2.0).

## Hosting DLL

The `../hosting/Microsoft.Extensions.Hosting.dll` is also built from PR #128788 and contains
the two-stage Host.StartAsync() orchestration (sync → async). However, it requires matching
.NET 11 dependency versions not yet available via NuGet, so it cannot be used as a runtime
override until the PR ships in a preview. See `src/Directory.Build.props` for details.

