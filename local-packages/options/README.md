# Options Local Packages (Tier 2 / Tier 2b)

These DLLs are built from the `dotnet/runtime` repository, `async-validation` branch.

## Contents

| DLL | Purpose |
|-----|---------|
| `Microsoft.Extensions.Options.dll` | Contains `IAsyncValidateOptions<T>`, `IAsyncStartupValidator`, `AsyncStartupValidator`, `AsyncStartupValidatorOptions`, `ValidateOnStartAsync()` |
| `Microsoft.Extensions.Options.DataAnnotations.dll` | Contains `DataAnnotationValidateOptionsAsync<T>`, `ValidateDataAnnotationsAsync()` |
| `Microsoft.Extensions.Options.SourceGeneration.dll` | Roslyn analyzer/source generator — emits `Validate()` and `ValidateAsync()` for `[OptionsValidator]` classes that implement `IAsyncValidateOptions<T>` |

## How to Rebuild

From the `dotnet/runtime` repo on the `async-validation` branch:

```powershell
# Option A: Full build (slow but populates all artifact paths)
build.cmd -subset libs -projects "src\libraries\Microsoft.Extensions.Options\src\Microsoft.Extensions.Options.csproj;src\libraries\Microsoft.Extensions.Options.DataAnnotations\src\Microsoft.Extensions.Options.DataAnnotations.csproj;src\libraries\Microsoft.Extensions.Options\gen\Microsoft.Extensions.Options.SourceGeneration.csproj" -c Debug

# Option B: Individual project builds (faster)
.\dotnet.cmd build src\libraries\Microsoft.Extensions.Options\src\Microsoft.Extensions.Options.csproj -c Debug
.\dotnet.cmd build src\libraries\Microsoft.Extensions.Options.DataAnnotations\src\Microsoft.Extensions.Options.DataAnnotations.csproj -c Debug
.\dotnet.cmd build src\libraries\Microsoft.Extensions.Options\gen\Microsoft.Extensions.Options.SourceGeneration.csproj -c Debug
```

Then copy from `artifacts\bin\`:
- `Microsoft.Extensions.Options\Debug\net11.0\Microsoft.Extensions.Options.dll`
- `Microsoft.Extensions.Options.DataAnnotations\Debug\net11.0\Microsoft.Extensions.Options.DataAnnotations.dll`
- `Microsoft.Extensions.Options.SourceGeneration\Debug\netstandard2.0\Microsoft.Extensions.Options.SourceGeneration.dll`

> **Note:** The source generator DLL must be the `netstandard2.0` build (Roslyn
> analyzers are required to target netstandard2.0). If using the full
> `build.cmd`, the DLL is also available at
> `microsoft.netcore.app.ref\analyzers\dotnet\cs\Microsoft.Extensions.Options.SourceGeneration.dll`.
